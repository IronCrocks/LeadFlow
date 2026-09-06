using System.ComponentModel.DataAnnotations;

namespace LeadFlow.Areas.Admin.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Введите email.")]
    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    [Display(Name = "Email")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; init; } = string.Empty;

    public string? ReturnUrl { get; init; }
}
