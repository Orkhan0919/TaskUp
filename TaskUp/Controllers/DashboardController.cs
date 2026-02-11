using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskUp.Data;
using TaskUp.Models;
using TaskUp.ViewModels;
using TaskUp.Utilities.Enums; // 👈 BoardType için ekle

namespace TaskUp.Controllers;

[Authorize] // 👈 Tüm sayfalar için login zorunlu!
public class DashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(AppDbContext context, UserManager<AppUser> userManager,ILogger<DashboardController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public class CreateBoardRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string BoardType { get; set; }
        public bool EnablePassword { get; set; }
        public string DashboardPassword { get; set; }
    }
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        // Kullanıcının panolarını getir (üye olduğu ve sahibi olduğu)
        var boards = await _context.Boards
            .Include(b => b.Members)
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
            .Where(b => b.OwnerId == user.Id || 
                        b.Members.Any(m => m.UserId == user.Id))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(boards); // Views/Dashboard/Index.cshtml
    }
    // ================================================

    [HttpGet]
    public IActionResult Access()
    {
        var model = new DashboardAccessVm
        {
            ShowDemoInfo = true 
        };
        return View(model);
    }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateBoard([FromBody] CreateBoardRequest request)
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Json(new { success = false, message = "Board name is required" });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(new { success = false, message = "User not found" });
        }

        string uniqueCode;
        do
        {
            uniqueCode = GenerateRandomCode(6);
        } 
        while (await _context.Boards.AnyAsync(b => b.JoinCode == uniqueCode));

        BoardType boardType = request.BoardType == "personal" ? BoardType.Personal : BoardType.Team;

        var newBoard = new Board
        {
            Name = request.Name,
            Description = request.Description ?? "",
            OwnerId = user.Id,
            JoinCode = uniqueCode,
            BoardType = boardType,
            IsPrivate = request.EnablePassword,
            Password = request.EnablePassword ? request.DashboardPassword : null,
            CreatedAt = DateTime.Now
        };

        _context.Boards.Add(newBoard);
        await _context.SaveChangesAsync();

        var ownerMember = new BoardMember
        {
            BoardId = newBoard.Id,
            UserId = user.Id,
            JoinedAt = DateTime.Now,
            Role = "Admin"
        };

        _context.BoardMembers.Add(ownerMember);
        await _context.SaveChangesAsync();

        return Json(new { success = true, boardId = newBoard.Id });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Create board error");
        
        string errorMessage = ex.Message;
        if (ex.InnerException != null)
        {
            errorMessage += " | Inner: " + ex.InnerException.Message;
            if (ex.InnerException.InnerException != null)
            {
                errorMessage += " | Inner2: " + ex.InnerException.InnerException.Message;
            }
        }
        
        return Json(new { success = false, message = errorMessage });
    }
}
    [HttpGet]
    public IActionResult Join()
    {
        return View();  
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinBoard(DashboardAccessVm model)
    {
        if (string.IsNullOrWhiteSpace(model.AccessCode))
        {
            ModelState.AddModelError("AccessCode", "Please enter 6-digit participation code.");
            return View("Access", model);
        }

        var user = await _userManager.GetUserAsync(User);
        
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.JoinCode == model.AccessCode);

        if (board == null)
        {
            ModelState.AddModelError("AccessCode", "Bu kodla eşleşen bir pano bulunamadı.");
            return View("Access", model);
        }

        if (board.OwnerId == user.Id)
        {
            return RedirectToAction("Index", "Board", new { id = board.Id });
        }

        var isAlreadyMember = await _context.BoardMembers
            .AnyAsync(m => m.BoardId == board.Id && m.UserId == user.Id);

        if (isAlreadyMember)
        {
            return RedirectToAction("Index", "Board", new { id = board.Id });
        }

        if (board.IsPrivate)
        {
            if (string.IsNullOrWhiteSpace(model.Password) || model.Password != board.Password)
            {
                ModelState.AddModelError("Password", "Pano kilitli: Hatalı veya eksik şifre.");
                model.RequiresPassword = true; 
                return View("Access", model);
            }
        }

        var newMember = new BoardMember
        {
            BoardId = board.Id,
            UserId = user.Id,
            JoinedAt = DateTime.Now,
            Role = "Member" 
        };

        _context.BoardMembers.Add(newMember);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Board", new { id = board.Id });
    }

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}