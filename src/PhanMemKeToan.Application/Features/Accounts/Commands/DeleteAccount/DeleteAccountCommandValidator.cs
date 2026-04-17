using FluentValidation;

namespace PhanMemKeToan.Application.Features.Accounts.Commands.DeleteAccount;

public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThanOrEqualTo(0);
    }
}
