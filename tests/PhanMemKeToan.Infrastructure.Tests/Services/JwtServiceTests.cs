using FluentAssertions;
using Microsoft.Extensions.Configuration;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Infrastructure.Services;
using System.Security.Cryptography;

namespace PhanMemKeToan.Infrastructure.Tests.Services;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        using var rsa = RSA.Create(2048);
        // Use byte array exports (available since .NET Core 3.0) + manual PEM construction
        var privateKeyBytes = rsa.ExportPkcs8PrivateKey();
        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        var privateKeyPemStr = "-----BEGIN PRIVATE KEY-----\n"
            + Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks)
            + "\n-----END PRIVATE KEY-----";
        var publicKeyPemStr = "-----BEGIN PUBLIC KEY-----\n"
            + Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks)
            + "\n-----END PUBLIC KEY-----";
        var privateKeyPemBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(privateKeyPemStr));
        var publicKeyPemBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(publicKeyPemStr));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:AccessTokenTtlMinutes"] = "15",
                ["JwtSettings:RefreshTokenTtlDays"] = "7",
                ["JwtSettings:PrivateKeyPemBase64"] = privateKeyPemBase64,
                ["JwtSettings:PublicKeyPemBase64"] = publicKeyPemBase64,
            })
            .Build();

        _jwtService = new JwtService(config);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturn_NonEmptyToken()
    {
        var claims = new UserClaimsDto(
            Guid.NewGuid(), Guid.NewGuid(), "test@test.com",
            ["Admin"], ["SYS.Users.View"], Guid.NewGuid().ToString()
        );

        var token = _jwtService.GenerateAccessToken(claims);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturn_PlaintextAndHash()
    {
        var (plaintext, hash) = _jwtService.GenerateRefreshToken();

        plaintext.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBeNullOrWhiteSpace();
        plaintext.Should().NotBe(hash);
    }

    [Fact]
    public void ValidateToken_ValidToken_ShouldReturn_Claims()
    {
        var claims = new UserClaimsDto(
            Guid.NewGuid(), Guid.NewGuid(), "test@test.com",
            ["Admin"], ["SYS.Users.View"], Guid.NewGuid().ToString()
        );

        var token = _jwtService.GenerateAccessToken(claims);
        var principal = _jwtService.ValidateToken(token);

        principal.Should().NotBeNull();
    }

    [Fact]
    public void GetJwks_ShouldReturn_NonEmptyKeySet()
    {
        var jwks = _jwtService.GetJwks();
        jwks.Should().NotBeNull();
        jwks.Keys.Should().NotBeEmpty();
    }
}
