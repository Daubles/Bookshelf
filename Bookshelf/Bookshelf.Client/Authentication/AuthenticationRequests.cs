using System.ComponentModel.DataAnnotations;

namespace Bookshelf.Client.Authentication;

public class RegisterUserRequest
{
    [Required, StringLength(30, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Username may only contain letters, numbers, full stops, hyphens or underscores.")]
    public string Username { get; set; } = "";

    [Required, StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required, StringLength(254)] 
    [EmailAddress] 
    public string Email { get; set; } = "";

    [Required, StringLength(128, MinimumLength = 10)]
    public string Password { get; set; } = "";
}

public record LoginUserRequest
{
    [Required, StringLength(254)]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required] 
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
};
