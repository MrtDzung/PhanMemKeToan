using FluentValidation;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertExpenseItem;

public class UpsertExpenseItemCommandValidator : AbstractValidator<UpsertExpenseItemCommand>
{
    public UpsertExpenseItemCommandValidator()
    {
        RuleFor(x => x.ExpenseCode)
            .NotEmpty().WithMessage("Mã khoản mục chi phí không được để trống.")
            .MaximumLength(25).WithMessage("Mã khoản mục chi phí không được vượt quá 25 ký tự.");

        RuleFor(x => x.ExpenseName)
            .NotEmpty().WithMessage("Tên khoản mục chi phí không được để trống.")
            .MaximumLength(255).WithMessage("Tên khoản mục chi phí không được vượt quá 255 ký tự.");
    }
}
