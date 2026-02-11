using Microsoft.AspNetCore.Identity;
using TaskUp.Models;
using TaskUp.ViewModels.Identity;

namespace TaskUp.Services.Account;

public interface IAccountService
{
    Task<IdentityResult> RegisterAsync(RegisterVM model);
    Task<SignInResult> LoginAsync(LoginVM login);
    Task LogoutAsync();
    
    Task<IdentityResult> ConfirmEmailAsync(string email, string token);
    Task<bool> IsEmailConfirmedAsync(string email);
    Task ResendConfirmationEmailAsync(string email);
    
    Task ForgotPasswordAsync(string email);
    Task<IdentityResult> ResetPasswordAsync(ResetPasswordVM model);
    Task<IdentityResult> ChangePasswordAsync(AppUser user, string currentPassword, string newPassword);
    
    Task<AppUser> GetUserByEmailAsync(string email);
    Task<AppUser> GetUserByIdAsync(string userId);
    Task<bool> CheckPasswordAsync(AppUser user, string password);
    Task<IdentityResult> UpdateProfileAsync(AppUser user);
}