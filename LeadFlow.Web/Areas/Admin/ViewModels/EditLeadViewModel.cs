using System.ComponentModel.DataAnnotations;
using LeadFlow.Domain.Enums;

namespace LeadFlow.Areas.Admin.ViewModels;

public sealed class EditLeadViewModel
{
    [Required(ErrorMessage = "Выберите статус.")]
    public LeadStatus? Status { get; init; }

    [StringLength(4000, ErrorMessage = "Внутренний комментарий не должен быть длиннее 4000 символов.")]
    [Display(Name = "Внутренний комментарий")]
    public string? ManagerComment { get; init; }

    public EditLeadViewModel Normalize()
    {
        var comment = ManagerComment?.Trim();

        return new EditLeadViewModel
        {
            Status = Status,
            ManagerComment = string.IsNullOrWhiteSpace(comment) ? null : comment
        };
    }
}
