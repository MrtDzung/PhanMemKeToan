using FluentValidation;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.CreateTenant;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã công ty không được để trống.")
            .MaximumLength(50).WithMessage("Mã công ty tối đa 50 ký tự.")
            .Matches("^[A-Z0-9_]+$").WithMessage("Mã công ty chỉ chứa chữ in hoa, số và dấu gạch dưới.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên công ty không được để trống.")
            .MaximumLength(200).WithMessage("Tên công ty tối đa 200 ký tự.");

        RuleFor(x => x.ConnectionStringEncrypted)
            .NotEmpty().When(x => x.DatabaseMode == DatabaseMode.OnPremise)
            .WithMessage("Connection string bắt buộc cho chế độ On-Premise.");
    }
}
