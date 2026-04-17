using FluentValidation;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Accounts.Commands.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Mã tài khoản không được để trống.")
            .MaximumLength(20).WithMessage("Mã tài khoản không được vượt quá 20 ký tự.")
            .Matches("^[a-zA-Z0-9]+$").WithMessage("Mã tài khoản chỉ được chứa chữ cái và chữ số.");

        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Tên tài khoản không được để trống.")
            .MaximumLength(128).WithMessage("Tên tài khoản không được vượt quá 128 ký tự.");

        RuleFor(x => x.AccountNameEnglish)
            .MaximumLength(128).When(x => x.AccountNameEnglish != null)
            .WithMessage("Tên tài khoản tiếng Anh không được vượt quá 128 ký tự.");

        RuleFor(x => x.AccountObjectType)
            .NotEqual(AccountObjectType.None)
            .When(x => x.DetailByAccountObject)
            .WithMessage("Loại đối tượng hạch toán phải được chọn khi bật hạch toán theo đối tượng.");
    }
}
