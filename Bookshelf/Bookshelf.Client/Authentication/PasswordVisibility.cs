using MudBlazor;

namespace Bookshelf.Client.Authentication;

// Show/hide state for password fields; bind InputType and Icon, and call Toggle from the adornment click
public class PasswordVisibility
{
    private bool IsVisible { get; set; }

    public InputType InputType => IsVisible ? InputType.Text : InputType.Password;

    public string Icon => IsVisible ? Icons.Material.Filled.Visibility : Icons.Material.Filled.VisibilityOff;

    public void Toggle() => IsVisible = !IsVisible;
}
