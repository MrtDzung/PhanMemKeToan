using RefreshTokenEntity = PhanMemKeToan.Domain.Entities.RefreshToken;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Auth.Commands.RefreshToken;
using PhanMemKeToan.Application.Tests.TestHelpers;
using PhanMemKeToan.Domain.Common.Exceptions;

namespace PhanMemKeToan.Application.Tests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> _loggerMock = new();

    private RefreshTokenCommandHandler CreateHandler()
        => new(
            _dbContextMock.Object,
            _jwtServiceMock.Object,
            _configMock.Object,
            _loggerMock.Object);

    [Fact]
    public async Task Handle_ExpiredToken_Throws_TokenExpiredException()
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = Convert.ToHexString(
            sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes("plainRefreshToken"))
        ).ToLowerInvariant();

        var token = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            TokenHash = hash,
            TokenFamily = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1), // Expired
            IssuedAt = DateTimeOffset.UtcNow.AddDays(-8),
            IsRevoked = false,
            UserId = Guid.NewGuid()
        };

        SetupRefreshTokens([token]);

        var handler = CreateHandler();
        await Assert.ThrowsAsync<TokenExpiredException>(
            () => handler.Handle(new RefreshTokenCommand("plainRefreshToken"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RevokedToken_Throws_TokenRevokedException()
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = Convert.ToHexString(
            sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes("plainRefreshToken"))
        ).ToLowerInvariant();

        var tokenFamily = Guid.NewGuid();
        var token = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(), TokenHash = hash,
            TokenFamily = tokenFamily,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IssuedAt = DateTimeOffset.UtcNow,
            IsRevoked = true, // Revoked
            UserId = Guid.NewGuid()
        };

        SetupRefreshTokens([token]);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        await Assert.ThrowsAsync<TokenRevokedException>(
            () => handler.Handle(new RefreshTokenCommand("plainRefreshToken"), CancellationToken.None));
    }

    private void SetupRefreshTokens(IEnumerable<RefreshTokenEntity> tokens)
    {
        var queryable = tokens.AsQueryable();
        var dbSetMock = new Mock<DbSet<RefreshTokenEntity>>();
        dbSetMock.As<IQueryable<RefreshTokenEntity>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<RefreshTokenEntity>(queryable.Provider));
        dbSetMock.As<IQueryable<RefreshTokenEntity>>().Setup(m => m.Expression)
            .Returns(queryable.Expression);
        dbSetMock.As<IQueryable<RefreshTokenEntity>>().Setup(m => m.ElementType)
            .Returns(queryable.ElementType);
        dbSetMock.As<IQueryable<RefreshTokenEntity>>().Setup(m => m.GetEnumerator())
            .Returns(queryable.GetEnumerator());
        dbSetMock.As<IAsyncEnumerable<RefreshTokenEntity>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<RefreshTokenEntity>(queryable.GetEnumerator()));
        _dbContextMock.Setup(x => x.RefreshTokens).Returns(dbSetMock.Object);
    }
}
