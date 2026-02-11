using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskUp.Data;
using TaskUp.Models;
using TaskUp.ViewModels;
using TaskUp.Services.Email;
using TaskUp.Utilities.Enums;
using TaskUp.Services.Member;

namespace TaskUp.Controllers
{
    [Authorize]
    public class BoardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IMemberService _memberService;
        private readonly ILogger<BoardController> _logger;

        public BoardController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            IEmailService emailService,
            IMemberService memberService,
            ILogger<BoardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _memberService = memberService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isMember = await _memberService.IsMemberAsync(id, user.Id);
            var isBanned = await _memberService.IsBannedAsync(id, user.Id);
            
            if (isBanned)
            {
                TempData["Error"] = "You have been banned from this board.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (!isMember)
            {
                TempData["Error"] = "You don't have access to this board.";
                return RedirectToAction("Index", "Dashboard");
            }

            var board = await _context.Boards
                .Include(b => b.Columns.OrderBy(c => c.Order))
                    .ThenInclude(c => c.Tasks.OrderBy(t => t.Order))
                        .ThenInclude(t => t.Assignees)
                            .ThenInclude(a => a.User)
                .Include(b => b.Columns)
                    .ThenInclude(c => c.Tasks)
                        .ThenInclude(t => t.Comments)
                .Include(b => b.Columns)
                    .ThenInclude(c => c.Tasks)
                        .ThenInclude(t => t.Attachments)
                .Include(b => b.Members)
                    .ThenInclude(m => m.User)
                .Include(b => b.Owner)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (board == null) return NotFound();

            return View(board);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddColumn([FromBody] AddColumnRequest request)
        {
            try
            {
                var column = new BoardColumn
                {
                    Name = request.Name,
                    BoardId = request.BoardId,
                    Order = 0
                };

                _context.BoardColumns.Add(column);
                await _context.SaveChangesAsync();

                return Json(new { success = true, columnId = column.Id, columnName = column.Name });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class AddColumnRequest
        {
            public int BoardId { get; set; }
            public string Name { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask([FromBody] AddTaskViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                       .Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "Invalid data: " + string.Join(", ", errors) });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                var column = await _context.BoardColumns
                    .Include(c => c.Board)
                    .FirstOrDefaultAsync(c => c.Id == model.ColumnId);

                if (column == null)
                    return Json(new { success = false, message = "Column not found" });

                var isMember = await _memberService.IsMemberAsync(column.BoardId, user.Id);
                var isOwner = column.Board.OwnerId == user.Id;

                if (!isMember && !isOwner)
                    return Json(new { success = false, message = "Permission denied" });

                var maxOrder = await _context.BoardTasks
                    .Where(t => t.ColumnId == model.ColumnId)
                    .MaxAsync(t => (int?)t.Order) ?? 0;

                TaskPriority taskPriority = TaskPriority.Medium;
                if (!string.IsNullOrWhiteSpace(model.Priority))
                {
                    Enum.TryParse<TaskPriority>(model.Priority, true, out taskPriority);
                }

                var task = new BoardTask
                {
                    Title = model.Title,
                    Description = model.Description ?? "",
                    Priority = taskPriority,
                    DueDate = model.DueDate,
                    ColumnId = model.ColumnId,
                    Order = maxOrder + 1,
                    CreatedAt = DateTime.Now,
                    IsCompleted = false
                };

                _context.BoardTasks.Add(task);
                await _context.SaveChangesAsync();

                return Json(new { success = true, taskId = task.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Add task error");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTask(int taskId, string title, string description, string priority, DateTime? dueDate)
        {
            try
            {
                var task = await _context.BoardTasks.FindAsync(taskId);
                if (task == null)
                    return Json(new { success = false, message = "Task not found" });

                if (!string.IsNullOrWhiteSpace(title))
                    task.Title = title;
        
                if (description != null)
                    task.Description = description;
        
                if (!string.IsNullOrWhiteSpace(priority))
                {
                    if (Enum.TryParse<TaskPriority>(priority, true, out var priorityEnum))
                    {
                        task.Priority = priorityEnum;
                    }
                }
        
                task.DueDate = dueDate;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Task updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task update error");
                return Json(new { success = false, message = "Failed to update task" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            try
            {
                var task = await _context.BoardTasks.FindAsync(taskId);
                if (task == null)
                    return Json(new { success = false, message = "Task not found" });

                _context.BoardTasks.Remove(task);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Task deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task delete error");
                return Json(new { success = false, message = "Failed to delete task" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTask(int taskId)
        {
            try
            {
                var task = await _context.BoardTasks.FindAsync(taskId);
                if (task == null)
                    return Json(new { success = false, message = "Task not found" });

                task.IsCompleted = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Task completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task complete error");
                return Json(new { success = false, message = "Failed to complete task" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveTask([FromBody] MoveTaskViewModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                var task = await _context.BoardTasks
                    .Include(t => t.Column)
                    .ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == model.TaskId);

                if (task == null) 
                    return Json(new { success = false, message = "Task not found" });

                var isMember = await _memberService.IsMemberAsync(task.Column.BoardId, user.Id);
                var isOwner = task.Column.Board.OwnerId == user.Id;

                if (!isMember && !isOwner)
                    return Json(new { success = false, message = "Permission denied" });

                var targetColumn = await _context.BoardColumns
                    .FirstOrDefaultAsync(c => c.Id == model.ColumnId && c.BoardId == task.Column.BoardId);
            
                if (targetColumn == null)
                    return Json(new { success = false, message = "Target column not found" });

                task.ColumnId = model.ColumnId;
        
                var maxOrder = await _context.BoardTasks
                    .Where(t => t.ColumnId == model.ColumnId)
                    .MaxAsync(t => (int?)t.Order) ?? 0;
        
                task.Order = maxOrder + 1;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Move task error");
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTaskOrder([FromBody] UpdateTaskOrderViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var column = await _context.BoardColumns.FindAsync(model.ColumnId);

            if (column == null) 
                return Json(new { success = false, message = "Column not found" });

            var isMember = await _memberService.IsMemberAsync(column.BoardId, user.Id);

            if (!isMember) 
                return Json(new { success = false, message = "Permission denied" });

            foreach (var taskOrder in model.TaskOrders)
            {
                var task = await _context.BoardTasks.FindAsync(taskOrder.TaskId);
                if (task != null && task.ColumnId == model.ColumnId)
                {
                    task.Order = taskOrder.Order;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTask(int taskId, string assigneeEmail)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var task = await _context.BoardTasks
                    .Include(t => t.Column)
                        .ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return Json(new { success = false, message = "Task not found." });

                var assignee = await _userManager.FindByEmailAsync(assigneeEmail);
                if (assignee == null)
                    return Json(new { success = false, message = "User not found." });

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteMember(int boardId, string email)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var board = await _context.Boards
                    .Include(b => b.Members)
                    .FirstOrDefaultAsync(b => b.Id == boardId && b.OwnerId == user.Id);
            
                if (board == null)
                    return Json(new { success = false, message = "Board not found or you don't have permission." });

                var targetUser = await _userManager.FindByEmailAsync(email);
                
                var isMember = board.Members.Any(m => m.User.Email == email);
                if (isMember)
                    return Json(new { success = false, message = "User is already a member of this board." });

                await _emailService.SendInvitationEmailAsync(
                    email, 
                    user.DisplayName ?? user.UserName, 
                    board.Name, 
                    board.JoinCode
                );

                return Json(new { success = true, message = "Invitation sent successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invitation error for board {BoardId} to {Email}", boardId, email);
                return Json(new { success = false, message = "Failed to send invitation." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteMultipleMembers(int boardId, [FromBody] List<string> emails)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var board = await _context.Boards
                    .Include(b => b.Members)
                    .FirstOrDefaultAsync(b => b.Id == boardId && b.OwnerId == user.Id);
            
                if (board == null)
                    return Json(new { success = false, message = "Board not found or you don't have permission." });

                var sentCount = 0;
                var existingCount = 0;
                var invalidCount = 0;

                foreach (var email in emails)
                {
                    var isMember = board.Members.Any(m => m.User.Email == email);
                    var userExists = await _userManager.FindByEmailAsync(email) != null;

                    if (!isMember && userExists)
                    {
                        await _emailService.SendInvitationEmailAsync(
                            email, 
                            user.DisplayName ?? user.UserName, 
                            board.Name, 
                            board.JoinCode
                        );
                        sentCount++;
                    }
                    else if (isMember)
                    {
                        existingCount++;
                    }
                    else
                    {
                        invalidCount++;
                    }
                }

                return Json(new 
                { 
                    success = true, 
                    message = $"Invitations sent: {sentCount}, Already members: {existingCount}, Invalid emails: {invalidCount}" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk invitation error for board {BoardId}", boardId);
                return Json(new { success = false, message = "Failed to send invitations." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Members(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            
            var board = await _context.Boards
                .Include(b => b.Members)
                    .ThenInclude(m => m.User)
                .Include(b => b.Owner)
                .Include(b => b.BannedUsers)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (board == null) 
                return NotFound();

            var isMember = await _memberService.IsMemberAsync(id, user.Id);
            var isOwner = board.OwnerId == user.Id;
            var isBanned = await _memberService.IsBannedAsync(id, user.Id);
            
            if (isBanned)
            {
                TempData["Error"] = "You have been banned from this board.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (!isMember && !isOwner)
            {
                TempData["Error"] = "You don't have access to this board.";
                return RedirectToAction("Index", "Dashboard");
            }

            return View(board);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KickMember(int boardId, string userId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                var isOwner = await _memberService.IsOwnerAsync(boardId, currentUser.Id);
                if (!isOwner)
                    return Json(new { success = false, message = "Only the board owner can kick members." });

                await _memberService.KickMemberAsync(boardId, userId, currentUser.Id);
                
                return Json(new { success = true, message = "Member kicked successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kick member error");
                return Json(new { success = false, message = "Failed to kick member." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanMember(int boardId, string userId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                var isOwner = await _memberService.IsOwnerAsync(boardId, currentUser.Id);
                if (!isOwner)
                    return Json(new { success = false, message = "Only the board owner can ban members." });

                await _memberService.BanMemberAsync(boardId, userId, currentUser.Id);
                
                return Json(new { success = true, message = "User banned permanently." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ban member error");
                return Json(new { success = false, message = "Failed to ban member." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanMember(int boardId, string userId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                var isOwner = await _memberService.IsOwnerAsync(boardId, currentUser.Id);
                if (!isOwner)
                    return Json(new { success = false, message = "Only the board owner can unban members." });

                await _memberService.UnbanMemberAsync(boardId, userId);
                
                return Json(new { success = true, message = "User unbanned successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unban member error");
                return Json(new { success = false, message = "Failed to unban member." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int boardId, string userId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var board = await _context.Boards
                    .FirstOrDefaultAsync(b => b.Id == boardId && b.OwnerId == user.Id);

                if (board == null)
                    return Json(new { success = false, message = "Permission denied." });

                var member = await _context.BoardMembers
                    .FirstOrDefaultAsync(m => m.BoardId == boardId && m.UserId == userId);

                if (member != null)
                {
                    _context.BoardMembers.Remove(member);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Member removed successfully." });
                }

                return Json(new { success = false, message = "Member not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Remove member error");
                return Json(new { success = false, message = "Failed to remove member." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || email.Length < 3)
                    return Json(new { success = true, data = new List<object>() });

                var users = await _userManager.Users
                    .Where(u => u.Email.Contains(email) || u.UserName.Contains(email))
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.UserName,
                        u.DisplayName
                    })
                    .Take(10)
                    .ToListAsync();

                return Json(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User search error");
                return Json(new { success = false, message = "Failed to search users." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Settings(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var board = await _context.Boards
                .Include(b => b.Owner)
                .FirstOrDefaultAsync(b => b.Id == id);
            
            if (board == null) 
                return NotFound();

            if (board.OwnerId != user.Id)
            {
                TempData["Error"] = "Only the board owner can access settings.";
                return RedirectToAction("Index", new { id });
            }

            return View(board);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBoard(int id, string name, string description, bool? isPrivate)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var board = await _context.Boards
                    .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == user.Id);

                if (board == null)
                    return Json(new { success = false, message = "Board not found or permission denied." });

                if (!string.IsNullOrWhiteSpace(name))
                    board.Name = name;

                if (description != null)
                    board.Description = description;

                if (isPrivate.HasValue)
                    board.IsPrivate = isPrivate.Value;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Board updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update board error");
                return Json(new { success = false, message = "Failed to update board." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateJoinCode(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var board = await _context.Boards
                    .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == user.Id);

                if (board == null)
                    return Json(new { success = false, message = "Board not found or permission denied." });

                string newJoinCode;
                do
                {
                    newJoinCode = GenerateRandomCode(6);
                } 
                while (await _context.Boards.AnyAsync(b => b.JoinCode == newJoinCode));

                board.JoinCode = newJoinCode;
                await _context.SaveChangesAsync();

                return Json(new { success = true, joinCode = newJoinCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Regenerate join code error");
                return Json(new { success = false, message = "Failed to regenerate join code." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var board = await _context.Boards
                    .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == user.Id);

                if (board == null)
                    return Json(new { success = false, message = "Board not found or permission denied." });

                _context.Boards.Remove(board);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Board deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete board error");
                return Json(new { success = false, message = "Failed to delete board." });
            }
        }

        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}