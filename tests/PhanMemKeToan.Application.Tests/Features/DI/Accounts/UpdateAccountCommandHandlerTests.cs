using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Accounts.Commands.UpdateAccount;
using PhanMemKeToan.Application.Tests.TestHelpers;
using PhanMemKeToan.Domain.Entities;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Tests.Features.DI.Accounts;

public class UpdateAccountCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();
    private readonly Mock<IAccountCacheService> _cacheServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private readonly Guid _tenantId = Guid.NewGuid();

    private UpdateAccountCommandHandler CreateHandler() =>
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

    private Account MakeAccount(Guid? id = null, int rowVersion = 0, string accountNumber = "111")
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = _tenantId,
            AccountNumber = accountNumber,
            AccountName = "Tiền mặt",
            Grade = 1,
            IsParent = false,
            RowVersion = rowVersion,
        };

    private UpdateAccountCommand MakeCommand(
        Guid accountId,
        int rowVersion = 0,
        string accountNumber = "111",
        Guid? parentId = null) => new(
            Id: accountId,
            RowVersion: rowVersion,
            AccountNumber: accountNumber,
            AccountName: "Tiền mặt (updated)",
            AccountNameEnglish: null,
            ParentId: parentId,
            AccountCategoryKind: AccountCategoryKind.Debit,
            Inactive: false,
            IsPostableInForeignCurrency: false,
            DetailByAccountObject: false,
            AccountObjectType: AccountObjectType.None,
            DetailByBankAccount: false,
            DetailByJob: false,
            DetailByProjectWork: false,
            DetailByOrder: false,
            DetailByContract: false,
            DetailByExpenseItem: false,
            DetailByDepartment: false,
            DetailByListItem: false,
            DetailByPUContract: false);

    [Fact]
    public async Task Handle_ValidUpdate_Should_UpdateFieldsAndIncrementRowVersion()
    {
        // Arrange
        var account = MakeAccount(rowVersion: 0);
        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([account]);
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _cacheServiceMock.Setup(s => s.InvalidateTreeAsync(_tenantId)).Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = MakeCommand(account.Id, rowVersion: 0);

        // Act
        var newRowVersion = await handler.Handle(command, CancellationToken.None);

        // Assert
        newRowVersion.Should().Be(1); // incremented
        account.AccountName.Should().Be("Tiền mặt (updated)");
        account.RowVersion.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RowVersionMismatch_Should_ThrowConflictException()
    {
        // Arrange
        var account = MakeAccount(rowVersion: 2); // DB = 2
        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([account]);
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);

        var handler = CreateHandler();
        var command = MakeCommand(account.Id, rowVersion: 0); // stale

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_BR_DI01_AccountNumberChangedAndNoPrefix_Should_ThrowValidationException()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = new Account
        {
            Id = parentId,
            TenantId = _tenantId,
            AccountNumber = "111",
            AccountName = "Parent",
            Grade = 1,
            RowVersion = 0
        };
        var account = MakeAccount(accountNumber: "1111"); // currently valid child of "111"
        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([account, parent]);
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);

        var handler = CreateHandler();
        // Change to "211" which does NOT start with parent "111"
        var command = MakeCommand(account.Id, rowVersion: 0, accountNumber: "211", parentId: parentId);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ValidUpdate_Should_InvalidateCache()
    {
        // Arrange
        var account = MakeAccount(rowVersion: 0);
        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([account]);
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _cacheServiceMock.Setup(s => s.InvalidateTreeAsync(_tenantId)).Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = MakeCommand(account.Id, rowVersion: 0);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(s => s.InvalidateTreeAsync(_tenantId), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateToRootAccount_NullParent_Should_SetGradeToOne()
    {
        // Arrange
        var account = MakeAccount(rowVersion: 0, accountNumber: "111");
        account.Grade = 3; // was previously a deep child
        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([account]);
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _cacheServiceMock.Setup(s => s.InvalidateTreeAsync(_tenantId)).Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = MakeCommand(account.Id, rowVersion: 0, accountNumber: "111", parentId: null);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        account.Grade.Should().Be(1);
    }
}
