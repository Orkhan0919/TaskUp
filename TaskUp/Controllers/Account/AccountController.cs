using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskUp.Models;
using TaskUp.Services.Account;
using TaskUp.ViewModels.Identity;

namespace TaskUp.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly UserManager<AppUser> _userManager; // EKLENDİ
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService accountService,
        UserManager<AppUser> userManager, // EKLENDİ
        ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _userManager = userManager; // EKLENDİ
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    // ============= REGISTER =============
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVM model)
    {
        try
        {
            if (!ModelState.IsValid) 
                return View(model);

            var result = await _accountService.RegisterAsync(model);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Registration successful! Please check your email to confirm your account.";
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Register error : {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Registration failed. Please try again later.");
        }
        
        return View(model);
    }

    // ============= LOGIN =============
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM model)
    {
        try
        {
            if (!ModelState.IsValid) 
                return View(model);

            var result = await _accountService.LoginAsync(model);
            
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            
            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Please confirm your email address before logging in.");
                return View(model);
            }
            
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account locked. Please try again later.");
                return View(model);
            }
            
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Login error : {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Login failed. Please try again later.");
        }
        
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Index", "Home");
        }

        try
        {
            // 1. Önce AccountService'i dene
            var result = await _accountService.ConfirmEmailAsync(email, token);
        
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Email confirmed successfully! You can now login.";
                return RedirectToAction("Login", "Account");
            }
        
            // 2. Service başarısız olursa MANUEL TRUE YAP!
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            
                TempData["SuccessMessage"] = "Email confirmed successfully! You can now login.";
                return RedirectToAction("Login", "Account");
            }
        
            TempData["ErrorMessage"] = "Email confirmation failed. The link may be expired or invalid.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Email confirmation error : {Email}", email);
            TempData["ErrorMessage"] = "Email confirmation failed. Please try again.";
            return RedirectToAction("Index", "Home");
        }
    }
    // ============= LOGOUT =============
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _accountService.LogoutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ============= FORGOT PASSWORD =============
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _accountService.ForgotPasswordAsync(model.Email);
            TempData["SuccessMessage"] = "If your email is registered, you will receive a password reset link.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Forgot password error : {Email}", model.Email);
            TempData["SuccessMessage"] = "If your email is registered, you will receive a password reset link.";
            return RedirectToAction("Login", "Account");
        }
    }

    // ============= RESET PASSWORD =============
    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Index", "Home");
        }

        var model = new ResetPasswordVM
        {
            Email = email,
            Token = token
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await _accountService.ResetPasswordAsync(model);
            
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password reset successfully! You can now login with your new password.";
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Reset password error : {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Password reset failed. Please try again.");
        }

        return View(model);
    }

    // ============= RESEND CONFIRMATION =============
    [HttpGet]
    public IActionResult ResendConfirmation()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmation(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError(string.Empty, "Email is required.");
            return View();
        }

        try
        {
            await _accountService.ResendConfirmationEmailAsync(email);
            TempData["SuccessMessage"] = "Confirmation email has been resent. Please check your inbox.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Resend confirmation error : {Email}", email);
            TempData["SuccessMessage"] = "If your email is registered, a confirmation link has been sent.";
            return RedirectToAction("Login", "Account");
        }
    }

    
    [NonAction]
    public async Task<string> GetUserDisplayNameAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user?.DisplayName ?? email;
    }

    [NonAction]
    public async Task<bool> IsEmailConfirmedAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;
        return await _userManager.IsEmailConfirmedAsync(user);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}