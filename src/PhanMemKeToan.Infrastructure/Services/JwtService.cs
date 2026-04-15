using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenTtlMinutes;

    public JwtService(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        _issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is required");
        _audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is required");
        _accessTokenTtlMinutes = int.TryParse(jwtSettings["AccessTokenTtlMinutes"], out var ttl) ? ttl : 15;

        var privateKeyPem = jwtSettings["PrivateKeyPemBase64"]
            ?? throw new InvalidOperationException("JwtSettings:PrivateKeyPemBase64 is required");
        var publicKeyPem = jwtSettings["PublicKeyPemBase64"]
            ?? throw new InvalidOperationException("JwtSettings:PublicKeyPemBase64 is required");

        var privateRsa = RSA.Create();
        privateRsa.ImportFromPem(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyPem)));
        _privateKey = new RsaSecurityKey(privateRsa) { KeyId = "v1" };

        var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(publicKeyPem)));
        _publicKey = new RsaSecurityKey(publicRsa) { KeyId = "v1" };
    }

    public string GenerateAccessToken(UserClaimsDto claims)
    {
        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddMinutes(_accessTokenTtlMinutes);

        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, claims.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, claims.Email),
            new(JwtRegisteredClaimNames.Jti, claims.Jti),
            new("tid", claims.TenantId.ToString()),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        foreach (var role in claims.Roles)
            tokenClaims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in claims.Permissions)
            tokenClaims.Add(new Claim("permission", permission));

        var signingCredentials = new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: tokenClaims,
            notBefore: now.UtcDateTime,
            expires: expiry.UtcDateTime,
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string Plaintext, string Hash) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var plaintext = Convert.ToBase64String(randomBytes);
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintext)));
        return (plaintext, hash.ToLowerInvariant());
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _publicKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        };

        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
        }
        catch
        {
            return null;
        }
    }

    public JwksDto GetJwks()
    {
        var publicRsaParams = _publicKey.Rsa.ExportParameters(false);
        var jwk = new JwkDto(
            Kty: "RSA",
            Use: "sig",
            Kid: _publicKey.KeyId,
            N: Base64UrlEncoder.Encode(publicRsaParams.Modulus!),
            E: Base64UrlEncoder.Encode(publicRsaParams.Exponent!),
            Alg: "RS256"
        );
        return new JwksDto([jwk]);
    }
}
