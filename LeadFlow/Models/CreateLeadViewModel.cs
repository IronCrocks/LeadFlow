using System.ComponentModel.DataAnnotations;

namespace LeadFlow.Models;

public sealed class CreateLeadViewModel
{
    public Guid SubmissionId { get; init; }

    [Required(ErrorMessage = "Введите имя.")]
    [StringLength(100, ErrorMessage = "Имя не должно быть длиннее 100 символов.")]
    [Display(Name = "Имя")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Введите телефон.")]
    [StringLength(50, ErrorMessage = "Телефон не должен быть длиннее 50 символов.")]
    [Display(Name = "Телефон")]
    public string Phone { get; init; } = string.Empty;

    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    [StringLength(254, ErrorMessage = "Email не должен быть длиннее 254 символов.")]
    [Display(Name = "Email")]
    public string? Email { get; init; }

    [Required(ErrorMessage = "Выберите услугу.")]
    [StringLength(150, ErrorMessage = "Название услуги не должно быть длиннее 150 символов.")]
    [Display(Name = "Услуга")]
    public string Service { get; init; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Комментарий не должен быть длиннее 2000 символов.")]
    [Display(Name = "Комментарий")]
    public string? CustomerComment { get; init; }

    public CreateLeadViewModel Normalize()
    {
        return new CreateLeadViewModel
        {
            SubmissionId = SubmissionId,
            Name = NormalizeRequired(Name),
            Phone = NormalizeRequired(Phone),
            Email = NormalizeOptional(Email),
            Service = NormalizeRequired(Service),
            CustomerComment = NormalizeOptional(CustomerComment)
        };
    }

    private static string NormalizeRequired(string? value) => value?.Trim() ?? string.Empty;

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
