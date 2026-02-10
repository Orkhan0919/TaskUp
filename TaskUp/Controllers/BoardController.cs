using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskUp.Data;
using TaskUp.Models;
using TaskUp.ViewModels;

namespace TaskUp.Controllers
{
    public class BoardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BoardController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isMember = await _context.BoardMembers
                .AnyAsync(m => m.BoardId == id && m.UserId == user.Id);
            
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
                .FirstOrDefaultAsync(b => b.Id == id);

            if (board == null) return NotFound();

            return View(board);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddColumn(AddColumnViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Invalid data" });

            var user = await _userManager.GetUserAsync(User);
            var board = await _context.Boards.FindAsync(model.BoardId);

            if (board == null || board.OwnerId != user.Id)
                return Json(new { success = false, message = "Permission denied" });

            var maxOrder = await _context.BoardColumns
                .Where(c => c.BoardId == model.BoardId)
                .MaxAsync(c => (int?)c.Order) ?? 0;

            var column = new BoardColumn
            {
                Name = model.Name,
                BoardId = model.BoardId,
                Order = maxOrder + 1
            };

            _context.BoardColumns.Add(column);
            await _context.SaveChangesAsync();

            return Json(new { success = true, columnId = column.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(AddTaskViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Invalid data" });

            var user = await _userManager.GetUserAsync(User);
            var column = await _context.BoardColumns
                .Include(c => c.Board)
                .FirstOrDefaultAsync(c => c.Id == model.ColumnId);

            if (column == null) return Json(new { success = false, message = "Column not found" });

            var isMember = await _context.BoardMembers
                .AnyAsync(m => m.BoardId == column.BoardId && m.UserId == user.Id);
            
            if (!isMember) return Json(new { success = false, message = "Permission denied" });

            var maxOrder = await _context.BoardTasks
                .Where(t => t.ColumnId == model.ColumnId)
                .MaxAsync(t => (int?)t.Order) ?? 0;

            var task = new BoardTask
            {
                Title = model.Title,
                Description = model.Description,
                Priority = model.Priority,
                DueDate = model.DueDate,
                ColumnId = model.ColumnId,
                Order = maxOrder + 1,
                CreatedAt = DateTime.Now
            };

            _context.BoardTasks.Add(task);
            await _context.SaveChangesAsync();

            return Json(new { success = true, taskId = task.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveTask(MoveTaskViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var task = await _context.BoardTasks
                .Include(t => t.Column)
                .FirstOrDefaultAsync(t => t.Id == model.TaskId);

            if (task == null) return Json(new { success = false, message = "Task not found" });

            var isMember = await _context.BoardMembers
                .AnyAsync(m => m.BoardId == task.Column.BoardId && m.UserId == user.Id);

            if (!isMember) return Json(new { success = false, message = "Permission denied" });

            task.ColumnId = model.ColumnId;
            
            // Yeni sütunun en altına ekle
            var maxOrder = await _context.BoardTasks
                .Where(t => t.ColumnId == model.ColumnId)
                .MaxAsync(t => (int?)t.Order) ?? 0;
            
            task.Order = maxOrder + 1;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTaskOrder([FromBody] UpdateTaskOrderViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var column = await _context.BoardColumns.FindAsync(model.ColumnId);

            if (column == null) return Json(new { success = false, message = "Column not found" });

            var isMember = await _context.BoardMembers
                .AnyAsync(m => m.BoardId == column.BoardId && m.UserId == user.Id);

            if (!isMember) return Json(new { success = false, message = "Permission denied" });

            foreach (var taskOrder in model.TaskOrders)
            {
                var task = await _context.BoardTasks.FindAsync(taskOrder.TaskId);
                // Sadece aynı sütundaki taskların sırasını güncelle
                if (task != null && task.ColumnId == model.ColumnId)
                {
                    task.Order = taskOrder.Order;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        public async Task<IActionResult> Members(int id)
        {
            var board = await _context.Boards
                .Include(b => b.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (board == null) return NotFound();

            return View(board);
        }

        public async Task<IActionResult> Settings(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var board = await _context.Boards.FindAsync(id);
            
            if (board == null) return NotFound();

            if (board.OwnerId != user.Id)
            {
                TempData["Error"] = "Only the board owner can access settings.";
                return RedirectToAction("Index", new { id });
            }

            return View(board);
        }
    }
}