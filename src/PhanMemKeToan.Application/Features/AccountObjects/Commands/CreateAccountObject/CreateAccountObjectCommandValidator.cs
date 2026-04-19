using FluentValidation;
using PhanMemKeToan.Application.Features.AccountObjects.Commands.CreateAccountObject;

namespace PhanMemKeToan.Application.Features.AccountObjects.Commands.CreateAccountObject;

public class CreateAccountObjectCommandValidator : AbstractValidator<CreateAccountObjectCommand>
{
    public CreateAccountObjectCommandValidator()
    {
        RuleFor(x => x.ObjectCode).NotEmpty().MaximumLength(25);
        RuleFor(x => x.ObjectName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ObjectType).GreaterThanOrEqualTo(1).LessThanOrEqualTo(7)
            .WithMessage("ObjectType must be a valid combination of Customer(1), Vendor(2), Employee(4).");
        RuleFor(x => x.ObjectNameEnglish).MaximumLength(255).When(x => x.ObjectNameEnglish != null);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address != null);
        RuleFor(x => x.TaxCode).MaximumLength(50).When(x => x.TaxCode != null);
        RuleFor(x => x.Email).MaximumLength(255).EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone != null);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentTermDays).GreaterThanOrEqualTo(0);

        RuleForEach(x => x.BankAccounts).ChildRules(bank =>
        {
            bank.RuleFor(b => b.BankName).NotEmpty().MaximumLength(255);
            bank.RuleFor(b => b.AccountNumber).NotEmpty().MaximumLength(50);
            bank.RuleFor(b => b.SwiftCode).MaximumLength(20).When(b => b.SwiftCode != null);
        });

        RuleForEach(x => x.OpeningBalances).ChildRules(ob =>
        {
            ob.RuleFor(o => o.CurrencyId).NotEmpty();
            ob.RuleFor(o => o.ExchangeRate).GreaterThan(0);
        });

        When(x => x.EmployeeProfile != null, () =>
        {
            RuleFor(x => x.EmployeeProfile!.CitizenId)
                .MaximumLength(20).When(x => x.EmployeeProfile!.CitizenId != null);
            RuleFor(x => x.EmployeeProfile!.SocialInsuranceNumber)
                .MaximumLength(10).When(x => x.EmployeeProfile!.SocialInsuranceNumber != null);
            RuleFor(x => x.EmployeeProfile!.DependentCount).GreaterThanOrEqualTo(0);
        });
    }
}
