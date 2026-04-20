using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Infrastructure.Services;

public class TempTokenService(IConfiguration configuration) : ITempTokenService
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(
        configuration["JwtSettings:TempTokenSecret"]
            ?? throw new InvalidOperationException("JwtSettings:TempTokenSecret not configured"));
    private readonly int _ttlSeconds = int.TryParse(
        configuration["JwtSettings:TempTokenTtlSeconds"], out var ttl) ? ttl : 60;

    public string Generate(TempTokenClaims claims)
    {
        var payload = new TempTokenPayload(claims.UserId, claims.RememberMe, DateTimeOffset.UtcNow.AddSeconds(_ttlSeconds).ToUnixTimeSeconds());
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var payloadBase64 = Convert.ToBase64String(payloadBytes);

        using var hmac = new HMACSHA256(_key);
        var signatureBytes = hmac.ComputeHash(payloadBytes);
        var signatureBase64 = Convert.ToBase64String(signatureBytes);

        return $"{payloadBase64}.{signatureBase64}";
    }

    public TempTokenClaims? Validate(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 2)
            return null;

        var payloadBytes = Convert.FromBase64String(parts[0]);

        using var hmac = new HMACSHA256(_key);
        var expectedSignature = Convert.ToBase64String(hmac.ComputeHash(payloadBytes));
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(parts[1])))
            return null;

        var payload = JsonSerializer.Deserialize<TempTokenPayload>(payloadBytes);
        if (payload is null || DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.Exp)
            return null;

        return new TempTokenClaims(payload.Sub, payload.Rmb);
    }

    private record TempTokenPayload(Guid Sub, bool Rmb, long Exp);
}
