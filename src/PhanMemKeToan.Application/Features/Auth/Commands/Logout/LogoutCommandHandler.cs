using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler(
    IMasterDbContext masterDbContext,
    ITokenBlacklistService blacklistService,
    ILogger<LogoutCommandHandler> logger
) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // 1. Blacklist JTI
        if (request.TokenRemainingTtl > TimeSpan.Zero)
            await blacklistService.BlacklistAsync(request.Jti, request.TokenRemainingTtl, cancellationToken);

        // 2. Revoke refresh token in Master DB
        var refreshToken = await masterDbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == request.RefreshTokenHash, cancellationToken);

        if (refreshToken is not null)
        {
            refreshToken.IsRevoked = true;
            await masterDbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. FR-033 auth log
        logger.LogInformation(
            "AUTH_EVENT {EventType} userId={UserId} tenantId={TenantId} jti={Jti}",
            "LOGOUT", request.UserId, request.TenantId, request.Jti);
    }
}
