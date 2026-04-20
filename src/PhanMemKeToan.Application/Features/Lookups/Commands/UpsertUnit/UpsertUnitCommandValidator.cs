using FluentValidation;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertUnit;

public class UpsertUnitCommandValidator : AbstractValidator<UpsertUnitCommand>
{
    public UpsertUnitCommandValidator()
    {
        RuleFor(x => x.UnitCode)
            .NotEmpty().WithMessage("Mã đơn vị tính không được để trống.")
            .MaximumLength(25).WithMessage("Mã đơn vị tính không được vượt quá 25 ký tự.");

        RuleFor(x => x.UnitName)
            .NotEmpty().WithMessage("Tên đơn vị tính không được để trống.")
            .MaximumLength(100).WithMessage("Tên đơn vị tính không được vượt quá 100 ký tự.");
    }
}
