using System.ComponentModel.DataAnnotations;

namespace TaskUp.ViewModels.Identity;

public class ForgotPasswordVM
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; }
}