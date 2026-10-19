using System.ComponentModel.DataAnnotations;

namespace Bookshelf.Client.Authentication;

public class RegisterForm : RegisterUserRequest
{
    [Required, Compare(nameof(Password), ErrorMessage = "Passwords don't match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = "";

    public RegisterUserRequest ToRequest() => new()
    {
        Username = Username,
        FirstName = FirstName,
        Email = Email,
        Password = Password
    };
}