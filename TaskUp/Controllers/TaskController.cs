using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskUp.Data;
using TaskUp.Models;
using TaskUp.Services.Email;
using TaskUp.Utilities.Enums; // TaskPriority enum için

namespace TaskUp.Controllers;

[Authorize]
public class TaskController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<TaskController> _logger;

    public TaskController(
        AppDbContext context,
        UserManager<AppUser> userManager,
        IEmailService emailService,
        ILogger<TaskController> logger)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    // 📋 TASK ATAMA
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTask(int taskId, string assigneeEmail)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Json(new { success = false, message = "User not found." });

            var task = await _context.BoardTasks
                .Include(t => t.Column)
                    .ThenInclude(c => c.Board)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return Json(new { success = false, message = "Task not found." });

            var assignee = await _userManager.FindByEmailAsync(assigneeEmail);
            if (assignee == null)
                return Json(new { success = false, message = "User not found." });

            // Zaten atanmış mı kontrol et
            var existingAssignment = await _context.TaskAssignees
                .AnyAsync(ta => ta.TaskId == taskId && ta.UserId == assignee.Id);

            if (!existingAssignment)
            {
                var taskAssignee = new TaskAssignee
                {
                    TaskId = taskId,
                    UserId = assignee.Id,
                    AssignedAt = DateTime.Now
                };
                _context.TaskAssignees.Add(taskAssignee);
                await _context.SaveChangesAsync();
            }

            // Task atama emaili gönder
            await _emailService.SendTaskAssignmentEmailAsync(
                assignee.Email,
                currentUser.DisplayName ?? currentUser.UserName,
                task.Title,
                task.Column.Board.Name
            );

            return Json(new { success = true, message = "Task assigned successfully!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task assignment error for task {TaskId}", taskId);
            return Json(new { success = false, message = "Failed to assign task." });
        }
    }

    // 👤 KULLANICIDAN TASK'I AL
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnassignTask(int taskId, string assigneeEmail)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var task = await _context.BoardTasks.FindAsync(taskId);

            if (task == null)
                return Json(new { success = false, message = "Task not found." });

            var assignee = await _userManager.FindByEmailAsync(assigneeEmail);
            if (assignee == null)
                return Json(new { success = false, message = "User not found." });

            var assignment = await _context.TaskAssignees
                .FirstOrDefaultAsync(ta => ta.TaskId == taskId && ta.UserId == assignee.Id);

            if (assignment != null)
            {
                _context.TaskAssignees.Remove(assignment);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Task unassigned successfully!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task unassignment error");
            return Json(new { success = false, message = "Failed to unassign task." });
        }
    }

    // 💬 MENTION BİLDİRİMİ
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MentionUser(int taskId, string mentionedEmail, string comment)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Json(new { success = false, message = "User not found." });

            var task = await _context.BoardTasks
                .Include(t => t.Column)
                    .ThenInclude(c => c.Board)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return Json(new { success = false, message = "Task not found." });

            var mentionedUser = await _userManager.FindByEmailAsync(mentionedEmail);
            if (mentionedUser == null)
                return Json(new { success = false, message = "User not found." });

            // Comment'i kaydet
            var taskComment = new TaskComment
            {
                TaskId = taskId,
                UserId = currentUser.Id,
                Content = comment,
                CreatedAt = DateTime.Now
            };
            _context.TaskComments.Add(taskComment);
            await _context.SaveChangesAsync();

            // Mention notification emaili gönder
            await _emailService.SendMentionNotificationEmailAsync(
                mentionedUser.Email,
                currentUser.DisplayName ?? currentUser.UserName,
                task.Title,
                comment
            );

            return Json(new { success = true, message = "Mention notification sent!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mention notification error");
            return Json(new { success = false, message = "Failed to send mention notification." });
        }
    }

    // ✅ TASK'I TAMAMLA
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteTask(int taskId)
    {
        try
        {
            var task = await _context.BoardTasks.FindAsync(taskId);
            if (task == null)
                return Json(new { success = false, message = "Task not found." });

            task.IsCompleted = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Task completed!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task complete error");
            return Json(new { success = false, message = "Failed to complete task." });
        }
    }

    // 🔄 TASK'I YENİDEN AÇ
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReopenTask(int taskId)
    {
        try
        {
            var task = await _context.BoardTasks.FindAsync(taskId);
            if (task == null)
                return Json(new { success = false, message = "Task not found." });

            task.IsCompleted = false;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Task reopened!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task reopen error");
            return Json(new { success = false, message = "Failed to reopen task." });
        }
    }

    // 📝 TASK GÜNCELLE - BoardTask modeline uygun hale getirildi
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTask(int taskId, string title, string description, string priority, DateTime? dueDate)
    {
        try
        {
            var task = await _context.BoardTasks.FindAsync(taskId);
            if (task == null)
                return Json(new { success = false, message = "Task not found." });

            if (!string.IsNullOrWhiteSpace(title))
                task.Title = title;
            
            if (description != null)
                task.Description = description;
            
            // PRIORITY - String'ten Enum'a dönüşüm (BoardTask modeline uygun)
            if (!string.IsNullOrWhiteSpace(priority))
            {
                // TaskPriority enum'ına çevir (case-insensitive)
                if (Enum.TryParse<TaskPriority>(priority, true, out var priorityEnum))
                {
                    task.Priority = priorityEnum;
                }
                else
                {
                    return Json(new { success = false, message = "Invalid priority value. Use: Low, Medium, High, Urgent" });
                }
            }
            
            task.DueDate = dueDate;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Task updated successfully!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task update error");
            return Json(new { success = false, message = "Failed to update task." });
        }
    }

    // 🗑️ TASK SİL
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        try
        {
            var task = await _context.BoardTasks.FindAsync(taskId);
            if (task == null)
                return Json(new { success = false, message = "Task not found." });

            _context.BoardTasks.Remove(task);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Task deleted successfully!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task delete error");
            return Json(new { success = false, message = "Failed to delete task." });
        }
    }

    // 👥 TASK ATANMIŞ KULLANICILARI GETİR
    [HttpGet]
    public async Task<IActionResult> GetTaskAssignees(int taskId)
    {
        try
        {
            var assignees = await _context.TaskAssignees
                .Include(ta => ta.User)
                .Where(ta => ta.TaskId == taskId)
                .Select(ta => new
                {
                    ta.User.Id,
                    ta.User.Email,
                    ta.User.DisplayName,
                    ta.User.UserName
                })
                .ToListAsync();

            return Json(new { success = true, data = assignees });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get task assignees error");
            return Json(new { success = false, message = "Failed to get assignees." });
        }
    }
}