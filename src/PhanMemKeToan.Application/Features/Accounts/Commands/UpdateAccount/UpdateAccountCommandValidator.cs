using FluentValidation;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Accounts.Commands.UpdateAccount;

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.RowVersion).GreaterThanOrEqualTo(0)
            .WithMessage("RowVersion phải >= 0.");

        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Mã tài khoản không được để trống.")
            .MaximumLength(20).WithMessage("Mã tài khoản không được vượt quá 20 ký tự.")
            .Matches("^[a-zA-Z0-9]+$").WithMessage("Mã tài khoản chỉ được chứa chữ cái và chữ số.");

        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Tên tài khoản không được để trống.")
            .MaximumLength(128).WithMessage("Tên tài khoản không được vượt quá 128 ký tự.");

        RuleFor(x => x.AccountObjectType)
            .NotEqual(AccountObjectType.None)
            .When(x => x.DetailByAccountObject)
            .WithMessage("Loại đối tượng hạch toán phải được chọn khi bật hạch toán theo đối tượng.");
    }
}
