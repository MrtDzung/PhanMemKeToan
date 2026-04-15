using MediatR;

namespace PhanMemKeToan.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(
    string Jti,
    TimeSpan TokenRemainingTtl,
    string RefreshTokenHash,
    Guid UserId,
    Guid TenantId
) : IRequest;
