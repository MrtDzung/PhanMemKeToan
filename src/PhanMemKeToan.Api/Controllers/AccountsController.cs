using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.Accounts.Commands.CreateAccount;
using PhanMemKeToan.Application.Features.Accounts.Commands.DeleteAccount;
using PhanMemKeToan.Application.Features.Accounts.Commands.ImportStandardCoa;
using PhanMemKeToan.Application.Features.Accounts.Commands.UpdateAccount;
using PhanMemKeToan.Application.Features.Accounts.Queries.GetAccountById;
using PhanMemKeToan.Application.Features.Accounts.Queries.GetAccountTree;
using PhanMemKeToan.Application.Features.Accounts.Queries.SearchAccounts;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "DI.Accounts.View")]
    public async Task<IActionResult> GetAccounts(
        [FromQuery] string format = "tree",
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAccountTreeQuery(includeInactive, format), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpGet("search")]
    [Authorize(Policy = "DI.Accounts.View")]
    public async Task<IActionResult> SearchAccounts(
        [FromQuery] string q,
        [FromQuery] bool postableOnly = true,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SearchAccountsQuery(q, postableOnly, limit), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "DI.Accounts.View")]
    public async Task<IActionResult> GetAccount(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountByIdQuery(id), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpPost]
    [Authorize(Policy = "DI.Accounts.Manage")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAccountCommand(
            request.AccountNumber, request.AccountName, request.AccountNameEnglish,
            request.ParentId, request.AccountCategoryKind, request.IsPostableInForeignCurrency,
            request.DetailByAccountObject, request.AccountObjectType, request.DetailByBankAccount,
            request.DetailByJob, request.DetailByProjectWork, request.DetailByOrder,
            request.DetailByContract, request.DetailByExpenseItem, request.DetailByDepartment,
            request.DetailByListItem, request.DetailByPUContract);

        var (id, rowVersion) = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAccount), new { id }, new { data = new { id, rowVersion }, errors = Array.Empty<object>() });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "DI.Accounts.Manage")]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateAccountCommand(
            id, request.RowVersion, request.AccountNumber, request.AccountName, request.AccountNameEnglish,
            request.ParentId, request.AccountCategoryKind, request.Inactive, request.IsPostableInForeignCurrency,
            request.DetailByAccountObject, request.AccountObjectType, request.DetailByBankAccount,
            request.DetailByJob, request.DetailByProjectWork, request.DetailByOrder,
            request.DetailByContract, request.DetailByExpenseItem, request.DetailByDepartment,
            request.DetailByListItem, request.DetailByPUContract);

        var newRowVersion = await sender.Send(command, cancellationToken);
        return Ok(new { data = new { id, rowVersion = newRowVersion }, errors = Array.Empty<object>() });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DI.Accounts.Manage")]
    public async Task<IActionResult> DeleteAccount(Guid id, [FromQuery] int rowVersion, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAccountCommand(id, rowVersion), cancellationToken);
        return NoContent();
    }

    [HttpPost("import")]
    [Authorize(Policy = "DI.Accounts.Import")]
    public async Task<IActionResult> ImportCoa([FromBody] ImportCoaRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ImportStandardCoaCommand(request.Standard, request.ConflictResolution, request.DryRun), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }
}

public record CreateAccountRequest(
    string AccountNumber,
    string AccountName,
    string? AccountNameEnglish,
    Guid? ParentId,
    AccountCategoryKind AccountCategoryKind,
    bool IsPostableInForeignCurrency,
    bool DetailByAccountObject,
    AccountObjectType AccountObjectType,
    bool DetailByBankAccount,
    bool DetailByJob,
    bool DetailByProjectWork,
    bool DetailByOrder,
    bool DetailByContract,
    bool DetailByExpenseItem,
    bool DetailByDepartment,
    bool DetailByListItem,
    bool DetailByPUContract
);

public record UpdateAccountRequest(
    int RowVersion,
    string AccountNumber,
    string AccountName,
    string? AccountNameEnglish,
    Guid? ParentId,
    AccountCategoryKind AccountCategoryKind,
    bool Inactive,
    bool IsPostableInForeignCurrency,
    bool DetailByAccountObject,
    AccountObjectType AccountObjectType,
    bool DetailByBankAccount,
    bool DetailByJob,
    bool DetailByProjectWork,
    bool DetailByOrder,
    bool DetailByContract,
    bool DetailByExpenseItem,
    bool DetailByDepartment,
    bool DetailByListItem,
    bool DetailByPUContract
);

public record ImportCoaRequest(
    string Standard,
    string ConflictResolution,
    bool DryRun = false
);
