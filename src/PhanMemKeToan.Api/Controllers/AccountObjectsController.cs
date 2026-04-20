using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.AccountObjects.Commands.CreateAccountObject;
using PhanMemKeToan.Application.Features.AccountObjects.Commands.DeleteAccountObject;
using PhanMemKeToan.Application.Features.AccountObjects.Commands.UpdateAccountObject;
using PhanMemKeToan.Application.Features.AccountObjects.Queries.GetAccountObjectById;
using PhanMemKeToan.Application.Features.AccountObjects.Queries.GetAccountObjects;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/account-objects")]
[Authorize]
public class AccountObjectsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "DI.AccountObjects.View")]
    public async Task<IActionResult> GetAccountObjects(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] int typeFilter = 0,
        [FromQuery] string status = "all",
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "objectCode",
        [FromQuery] string sortDir = "asc",
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAccountObjectsQuery(page, pageSize, typeFilter, status, search, sortBy, sortDir), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "DI.AccountObjects.View")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountObjectByIdQuery(id), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpPost]
    [Authorize(Policy = "DI.AccountObjects.Manage")]
    public async Task<IActionResult> CreateAccountObject([FromBody] CreateAccountObjectRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAccountObjectCommand(
            request.ObjectCode, request.ObjectName, request.ObjectNameEnglish,
            request.Address, request.TaxCode, request.Email, request.Phone,
            request.Fax, request.Website, request.ContactPerson, request.ContactPhone,
            request.Description, request.ObjectType, request.CreditLimit, request.PaymentTermDays,
            request.IsActive, request.AccountObjectGroupId,
            request.BankAccounts, request.OpeningBalances, request.EmployeeProfile);

        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new { data = new { id = result.Id, rowVersion = result.RowVersion }, errors = Array.Empty<object>() });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "DI.AccountObjects.Manage")]
    public async Task<IActionResult> UpdateAccountObject(Guid id, [FromBody] UpdateAccountObjectRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateAccountObjectCommand(
            id, request.RowVersion,
            request.ObjectCode, request.ObjectName, request.ObjectNameEnglish,
            request.Address, request.TaxCode, request.Email, request.Phone,
            request.Fax, request.Website, request.ContactPerson, request.ContactPhone,
            request.Description, request.ObjectType, request.CreditLimit, request.PaymentTermDays,
            request.IsActive, request.AccountObjectGroupId,
            request.BankAccounts, request.OpeningBalances, request.EmployeeProfile,
            request.ConfirmRemoveProfile);

        var result = await sender.Send(command, cancellationToken);
        return Ok(new { data = new { id = result.Id, rowVersion = result.RowVersion }, errors = Array.Empty<object>() });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DI.AccountObjects.Manage")]
    public async Task<IActionResult> DeleteAccountObject(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAccountObjectCommand(id), cancellationToken);
        return NoContent();
    }
}

public record CreateAccountObjectRequest(
    string ObjectCode,
    string ObjectName,
    string? ObjectNameEnglish,
    string? Address,
    string? TaxCode,
    string? Email,
    string? Phone,
    string? Fax,
    string? Website,
    string? ContactPerson,
    string? ContactPhone,
    string? Description,
    int ObjectType,
    decimal CreditLimit,
    int PaymentTermDays,
    bool IsActive,
    Guid? AccountObjectGroupId,
    List<CreateBankAccountDto> BankAccounts,
    List<CreateOpeningBalanceDto> OpeningBalances,
    CreateEmployeeProfileDto? EmployeeProfile
);

public record UpdateAccountObjectRequest(
    int RowVersion,
    string ObjectCode,
    string ObjectName,
    string? ObjectNameEnglish,
    string? Address,
    string? TaxCode,
    string? Email,
    string? Phone,
    string? Fax,
    string? Website,
    string? ContactPerson,
    string? ContactPhone,
    string? Description,
    int ObjectType,
    decimal CreditLimit,
    int PaymentTermDays,
    bool IsActive,
    Guid? AccountObjectGroupId,
    List<UpdateBankAccountDto> BankAccounts,
    List<UpdateOpeningBalanceDto> OpeningBalances,
    UpdateEmployeeProfileDto? EmployeeProfile,
    bool ConfirmRemoveProfile = false
);
