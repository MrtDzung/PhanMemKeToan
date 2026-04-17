using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Accounts.Commands.CreateAccount;
using PhanMemKeToan.Application.Tests.TestHelpers;
using PhanMemKeToan.Domain.Entities;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Tests.Features.DI.Accounts;

public class CreateAccountCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();
    private readonly Mock<IAccountCacheService> _cacheServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private readonly Guid _tenantId = Guid.NewGuid();

    private CreateAccountCommandHandler CreateHandler() =>
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

    private static CreateAccountCommand ValidCommand(
        string accountNumber = "1111",
        Guid? parentId = null) => new(
            AccountNumber: accountNumber,
            AccountName: "Tiền mặt",
            AccountNameEnglish: null,
            ParentId: parentId,
            AccountCategoryKind: AccountCategoryKind.Debit,
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
    public async Task Handle_ValidRootAccount_Should_ReturnGuidAndRowVersion()
    {
        // Arrange
        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([]);
        dbSet.Setup(d => d.Add(It.IsAny<Account>()));
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _cacheServiceMock.Setup(s => s.InvalidateTreeAsync(_tenantId)).Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = ValidCommand("111");

        // Act
        var (id, rowVersion) = await handler.Handle(command, CancellationToken.None);

        // Assert
        id.Should().NotBeEmpty();
        rowVersion.Should().Be(0);
        dbSet.Verify(d => d.Add(It.Is<Account>(a =>
            a.AccountNumber == "111" &&
            a.TenantId == _tenantId &&
            a.Grade == 1)), Times.Once);
    }

    [Fact]
    public async Task Handle_BR_DI01_ChildNotStartingWithParent_Should_ThrowValidationException()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = new Account
        {
            Id = parentId,
            TenantId = _tenantId,
            AccountNumber = "111",
            AccountName = "Parent Account",
            Grade = 1,
            IsParent = true,
            RowVersion = 0
        };

        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([parent]);
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);

        var handler = CreateHandler();
        // "211" does NOT start with "111" → BR-DI01 violation
        var command = ValidCommand("211", parentId);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("AccountNumber");
    }

    [Fact]
    public async Task Handle_DuplicateAccountNumber_Should_ThrowValidationException()
    {
        // Arrange
        var existing = new Account
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AccountNumber = "111",
            AccountName = "Existing",
            Grade = 1,
            RowVersion = 0
        };

        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([existing]);
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);

        var handler = CreateHandler();
        var command = ValidCommand("111"); // duplicate

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("AccountNumber");
        ex.Which.Errors["AccountNumber"].Should().Contain(s => s.Contains("đã tồn tại"));
    }

    [Fact]
    public async Task Handle_ValidChildAccount_Should_SetGradeAndInvalidateCache()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = new Account
        {
            Id = parentId,
            TenantId = _tenantId,
            AccountNumber = "111",
            AccountName = "Parent",
            Grade = 2,
            IsParent = false,
            RowVersion = 0
        };

        _tenantContextMock.Setup(t => t.TenantId).Returns(_tenantId);
        var dbSet = SetupAccountsDbSet([parent]);
        dbSet.Setup(d => d.Add(It.IsAny<Account>()));
        _dbContextMock.Setup(c => c.Accounts).Returns(dbSet.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _cacheServiceMock.Setup(s => s.InvalidateTreeAsync(_tenantId)).Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = ValidCommand("1111", parentId); // starts with "111" — valid

        // Act
        var (id, rowVersion) = await handler.Handle(command, CancellationToken.None);

        // Assert
        id.Should().NotBeEmpty();
        dbSet.Verify(d => d.Add(It.Is<Account>(a =>
            a.Grade == 3 && // parent.Grade + 1
            a.ParentID == parentId)), Times.Once);
        // Parent.IsParent should be set to true
        parent.IsParent.Should().BeTrue();
        _cacheServiceMock.Verify(s => s.InvalidateTreeAsync(_tenantId), Times.Once);
    }

    [Fact]
    public async Task Handle_NoTenantId_Should_ThrowForbiddenAccessException()
    {
        // Arrange
        _tenantContextMock.Setup(t => t.TenantId).Returns((Guid?)null);

        var handler = CreateHandler();
        var command = ValidCommand();

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
