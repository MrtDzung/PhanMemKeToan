using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Accounts.Commands.DeleteAccount;
using PhanMemKeToan.Application.Tests.TestHelpers;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Tests.Features.DI.Accounts;

public class DeleteAccountCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();
    private readonly Mock<IAccountCacheService> _cacheServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private readonly Guid _tenantId = Guid.NewGuid();

    private DeleteAccountCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _tenantContextMock.Object, _cacheServiceMock.Object, _currentUserServiceMock.Object);

    private static Mock<DbSet<Account>> SetupAccountsDbSet(IEnumerable<Account> data)
    {
        var list = data.AsQueryable();
        var mock = new Mock<DbSet<Account>>();
        mock.As<IQueryable<Account>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Account>(list.Provider));
        mock.As<IQueryable<Account>>().Setup(m => m.Expression).Returns(list.Expression);
        mock.As<IQueryable<Account>>().Setup(m => m.ElementType).Returns(list.ElementType);
        mock.As<IQueryable<Account>>().Setup(m => m.GetEnumerator()).Returns(list.GetEnumerator());
        mock.As<IAsyncEnumerable<Account>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<Account>(list.GetEnumerator()));
        return mock;
    }

    private Account MakeAccount(Guid? id = null, int rowVersion = 0, bool hasChild = false, Guid? parentId = null)
    {
        var accountId = id ?? Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            TenantId = _tenantId,
            AccountNumber = "111",
            AccountName = "Tiền mặt",
            Grade = 1,
            IsParent = hasChild,
            RowVersion = rowVersion,
            ParentID = parentId
        };

        if (hasChild)
        {
            account.Children = [new Account
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                AccountNumber = "1111",
                AccountName = "Child",
                Grade = 2,
                ParentID = accountId,
                RowVersion = 0,
                IsDeleted = false
            }];
        }

        return account;
    }

    [Fact]
    public async Task Handle_ValidAccount_Should_SoftDeleteAndSetInactive()
    {
        // Arrange
        var account = MakeAccount(rowVersion: 0);
        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([account]);
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _cacheServiceMock.Setup(s => s.InvalidateTreeAsync(_tenantId)).Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new DeleteAccountCommand(account.Id, RowVersion: 0);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        account.IsDeleted.Should().BeTrue();
        account.Inactive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AccountHasChildren_Should_ThrowBusinessRuleException()
    {
        // Arrange
        var account = MakeAccount(hasChild: true, rowVersion: 0);
        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([account]);
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);

        var handler = CreateHandler();
        var command = new DeleteAccountCommand(account.Id, RowVersion: 0);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.Code.Should().Be("has_children");
    }

    [Fact]
    public async Task Handle_RowVersionMismatch_Should_ThrowConflictException()
    {
        // Arrange
        var account = MakeAccount(rowVersion: 1); // DB has rowVersion=1
        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([account]);
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);

        var handler = CreateHandler();
        var command = new DeleteAccountCommand(account.Id, RowVersion: 0); // stale rowVersion

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_AccountNotFound_Should_ThrowNotFoundException()
    {
        // Arrange
        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([]); // empty
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);

        var handler = CreateHandler();
        var command = new DeleteAccountCommand(Guid.NewGuid(), RowVersion: 0);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidDelete_Should_InvalidateCache()
    {
        // Arrange
        var account = MakeAccount(rowVersion: 0);
        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([account]);
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _cacheServiceMock.Setup(s => s.InvalidateTreeAsync(_tenantId)).Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new DeleteAccountCommand(account.Id, RowVersion: 0);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(s => s.InvalidateTreeAsync(_tenantId), Times.Once);
    }
}
