using FluentValidation;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertCurrency;

public class UpsertCurrencyCommandValidator : AbstractValidator<UpsertCurrencyCommand>
{
    public UpsertCurrencyCommandValidator()
    {
        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Mã tiền tệ không được để trống.")
            .MaximumLength(10).WithMessage("Mã tiền tệ không được vượt quá 10 ký tự.");

        RuleFor(x => x.CurrencyName)
            .NotEmpty().WithMessage("Tên tiền tệ không được để trống.")
            .MaximumLength(150).WithMessage("Tên tiền tệ không được vượt quá 150 ký tự.");

        RuleFor(x => x.ExchangeRate)
            .GreaterThan(0).WithMessage("Tỷ giá phải lớn hơn 0.");
    }
}
