using FluentValidation;

namespace PhanMemKeToan.Application.Features.AccountObjects.Commands.DeleteAccountObject;

public class DeleteAccountObjectCommandValidator : AbstractValidator<DeleteAccountObjectCommand>
{
    public DeleteAccountObjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
