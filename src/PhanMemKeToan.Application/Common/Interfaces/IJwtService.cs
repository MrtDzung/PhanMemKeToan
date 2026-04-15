using System.Security.Claims;

namespace PhanMemKeToan.Application.Common.Interfaces;

public record UserClaimsDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string Jti
);

public record JwkDto(string Kty, string Use, string Kid, string N, string E, string Alg);

public record JwksDto(IReadOnlyList<JwkDto> Keys);

public interface IJwtService
{
    string GenerateAccessToken(UserClaimsDto claims);
    (string Plaintext, string Hash) GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    JwksDto GetJwks();
}
