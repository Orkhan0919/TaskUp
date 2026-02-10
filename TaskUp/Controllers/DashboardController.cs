using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskUp.Data;
using TaskUp.Models;
using TaskUp.ViewModels;

namespace TaskUp.Controllers;

public class DashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public DashboardController(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

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
    public async Task<IActionResult> CreateBoard(DashboardAccessVm model)
    {
        if (!ModelState.IsValid)
        {
            model.ShowDemoInfo = true;
            return View("Access", model);
        }
        
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Pano ismi zorunludur.");
            return View("Access", model);
        }

        if (model.EnablePassword && string.IsNullOrWhiteSpace(model.DashboardPassword))
        {
            ModelState.AddModelError("DashboardPassword", "Kilitli pano için bir şifre belirlemelisiniz.");
            return View("Access", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        string uniqueCode;
        do
        {
            uniqueCode = GenerateRandomCode(6);
        } 
        while (await _context.Boards.AnyAsync(b => b.JoinCode == uniqueCode));

        var newBoard = new Board
        {
            Name = model.Name,
            Description = model.Description,
            OwnerId = user.Id,
            JoinCode = uniqueCode,
            IsPrivate = model.EnablePassword,
            Password = model.EnablePassword ? model.DashboardPassword : null, 
            CreatedAt = DateTime.Now
        };

        _context.Boards.Add(newBoard);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Board", new { id = newBoard.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinBoard(DashboardAccessVm model)
    {
        if (string.IsNullOrWhiteSpace(model.AccessCode))
        {
            ModelState.AddModelError("AccessCode", "Lütfen 6 haneli katılım kodunu girin.");
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
            JoinedAt = DateTime.Now
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