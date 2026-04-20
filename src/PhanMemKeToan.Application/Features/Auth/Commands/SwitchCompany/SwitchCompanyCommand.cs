using MediatR;

namespace PhanMemKeToan.Application.Features.Auth.Commands.SwitchCompany;

public record SwitchCompanyCommand(
    Guid UserId,
    Guid CurrentTenantId,
    Guid TargetTenantId,
    string IpAddress
) : IRequest<SwitchCompanyResult>;

public record SwitchCompanyResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
