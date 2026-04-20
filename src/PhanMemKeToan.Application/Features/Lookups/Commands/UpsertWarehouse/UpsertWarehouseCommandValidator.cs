using FluentValidation;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertWarehouse;

public class UpsertWarehouseCommandValidator : AbstractValidator<UpsertWarehouseCommand>
{
    public UpsertWarehouseCommandValidator()
    {
        RuleFor(x => x.WarehouseCode)
            .NotEmpty().WithMessage("Mã kho không được để trống.")
            .MaximumLength(25).WithMessage("Mã kho không được vượt quá 25 ký tự.");

        RuleFor(x => x.WarehouseName)
            .NotEmpty().WithMessage("Tên kho không được để trống.")
            .MaximumLength(255).WithMessage("Tên kho không được vượt quá 255 ký tự.");

        RuleFor(x => x.Address)
            .MaximumLength(500).When(x => x.Address != null)
            .WithMessage("Địa chỉ không được vượt quá 500 ký tự.");
    }
}
