using MediatR;

namespace PhanMemKeToan.Application.Features.Auth.Commands.SelectCompany;

public record SelectCompanyCommand(
    string TempToken,
    Guid TenantId,
    bool RememberMe,
    string IpAddress
) : IRequest<SelectCompanyResult>;

public record SelectCompanyResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
