using FluentValidation;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertDepartment;

public class UpsertDepartmentCommandValidator : AbstractValidator<UpsertDepartmentCommand>
{
    public UpsertDepartmentCommandValidator()
    {
        RuleFor(x => x.DepartmentCode)
            .NotEmpty().WithMessage("Mã bộ phận không được để trống.")
            .MaximumLength(25).WithMessage("Mã bộ phận không được vượt quá 25 ký tự.");

        RuleFor(x => x.DepartmentName)
            .NotEmpty().WithMessage("Tên bộ phận không được để trống.")
            .MaximumLength(255).WithMessage("Tên bộ phận không được vượt quá 255 ký tự.");
    }
}
