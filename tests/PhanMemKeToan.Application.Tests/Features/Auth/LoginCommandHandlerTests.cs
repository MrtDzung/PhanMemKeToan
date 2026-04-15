using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Auth.Commands.Login;
using PhanMemKeToan.Application.Tests.TestHelpers;
using PhanMemKeToan.Domain.Common.Exceptions;
using PhanMemKeToan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PhanMemKeToan.Application.Tests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock = new();

    private LoginCommandHandler CreateHandler()
        => new(
            _dbContextMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object,
            _cacheMock.Object,
            _configMock.Object,
            _loggerMock.Object);

    [Fact]
    public async Task Handle_InvalidEmail_Throws_InvalidCredentialsException()
    {
        // Arrange: empty Users set
        var users = new List<User>().AsQueryable();
        var dbSetMock = MockDbSet(users);
        _dbContextMock.Setup(x => x.Users).Returns(dbSetMock.Object);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(new LoginCommand("noone@example.com", "pass", false, "1.1.1.1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DeactivatedUser_Throws_AccountDeactivatedException()
    {
        var tenantId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Email = "user@example.com", PasswordHash = "hash",
            FullName = "User", IsActive = false
        };
        var users = new List<User> { user }.AsQueryable();
        var dbSetMock = MockDbSet(users);
        _dbContextMock.Setup(x => x.Users).Returns(dbSetMock.Object);

        var handler = CreateHandler();
        await Assert.ThrowsAsync<AccountDeactivatedException>(
            () => handler.Handle(new LoginCommand("user@example.com", "pass", false, "1.1.1.1"), CancellationToken.None));
    }

    private static Mock<DbSet<T>> MockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mock = new Mock<DbSet<T>>();
        mock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
        mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        mock.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        return mock;
    }
}
