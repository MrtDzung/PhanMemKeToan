using FluentValidation;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.UpdateTenant;

public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên công ty không được để trống.")
            .MaximumLength(200).WithMessage("Tên công ty tối đa 200 ký tự.");

        RuleFor(x => x.ConnectionStringEncrypted)
            .NotEmpty().When(x => x.DatabaseMode == DatabaseMode.OnPremise)
            .WithMessage("Connection string bắt buộc cho chế độ On-Premise.");
    }
}
