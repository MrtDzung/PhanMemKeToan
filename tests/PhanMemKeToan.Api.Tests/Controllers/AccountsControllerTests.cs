using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PhanMemKeToan.Api.Controllers;
using PhanMemKeToan.Application.Features.Accounts.DTOs;
using PhanMemKeToan.Application.Features.Accounts.Queries.GetAccountTree;

namespace PhanMemKeToan.Api.Tests.Controllers;

/// <summary>
/// Unit tests for AccountsController — verifies authorization attributes and action logic.
/// Integration-level auth (401) is verified by checking [Authorize] attribute presence.
/// Action responses are verified by mocking ISender.
/// </summary>
public class AccountsControllerTests
{
    private readonly Mock<ISender> _senderMock = new();

    private AccountsController CreateController() => new(_senderMock.Object);

    // ── Authorization attribute checks ─────────────────────────────────────

    [Fact]
    public void AccountsController_Class_Should_Have_AuthorizeAttribute()
    {
        // Arrange & Act
        var attr = typeof(AccountsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);

        // Assert — class-level [Authorize] ensures unauthenticated → 401
        attr.Should().NotBeEmpty("AccountsController must require authentication");
    }

    [Fact]
    public void GetAccounts_Action_Should_Have_AuthorizeAttribute_WithPolicy()
    {
        // Arrange & Act
        var method = typeof(AccountsController).GetMethod(nameof(AccountsController.GetAccounts));
        var attr = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull();
        attr!.Policy.Should().Be("DI.Accounts.View");
    }

    [Fact]
    public void CreateAccount_Action_Should_Have_AuthorizeAttribute_WithManagePolicy()
    {
        var method = typeof(AccountsController).GetMethod(nameof(AccountsController.CreateAccount));
        var attr = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        attr.Should().NotBeNull();
        attr!.Policy.Should().Be("DI.Accounts.Manage");
    }

    [Fact]
    public void DeleteAccount_Action_Should_Have_AuthorizeAttribute_WithManagePolicy()
    {
        var method = typeof(AccountsController).GetMethod(nameof(AccountsController.DeleteAccount));
        var attr = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        attr.Should().NotBeNull();
        attr!.Policy.Should().Be("DI.Accounts.Manage");
    }

    // ── Action logic tests ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAccounts_AuthorizedRequest_Should_Return200WithAccountList()
    {
        // Arrange
        var accounts = new List<AccountTreeNodeDto>
        {
            new() { AccountId = Guid.NewGuid(), AccountNumber = "111", AccountName = "Tiền mặt", Grade = 1 },
            new() { AccountId = Guid.NewGuid(), AccountNumber = "112", AccountName = "Tiền gửi", Grade = 1 },
        };

        _senderMock
            .Setup(s => s.Send(It.IsAny<GetAccountTreeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.GetAccounts("tree", false, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value!;
        var dataProp = value.GetType().GetProperty("data");
        dataProp.Should().NotBeNull();
        var data = dataProp!.GetValue(value) as IEnumerable<AccountTreeNodeDto>;
        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAccounts_EmptyTenant_Should_Return200WithEmptyList()
    {
        // Arrange
        _senderMock
            .Setup(s => s.Send(It.IsAny<GetAccountTreeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountTreeNodeDto>());

        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.GetAccounts("tree", false, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value!;
        var dataProp = value.GetType().GetProperty("data");
        var data = dataProp!.GetValue(value) as IEnumerable<AccountTreeNodeDto>;
        data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccounts_SendsCorrectQuery_WithQueryParams()
    {
        // Arrange
        _senderMock
            .Setup(s => s.Send(It.IsAny<GetAccountTreeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountTreeNodeDto>());

        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        await controller.GetAccounts("flat", includeInactive: true, CancellationToken.None);

        // Assert — correct query params forwarded to MediatR
        _senderMock.Verify(s => s.Send(
            It.Is<GetAccountTreeQuery>(q => q.IncludeInactive == true && q.Format == "flat"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
