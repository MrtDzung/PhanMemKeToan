using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.Tenants.Commands.CreateTenant;
using PhanMemKeToan.Application.Features.Tenants.Commands.GrantTenantAccess;
using PhanMemKeToan.Application.Features.Tenants.Commands.RevokeTenantAccess;
using PhanMemKeToan.Application.Features.Tenants.Commands.ToggleTenantStatus;
using PhanMemKeToan.Application.Features.Tenants.Commands.UpdateTenant;
using PhanMemKeToan.Application.Features.Tenants.Queries.GetTenantById;
using PhanMemKeToan.Application.Features.Tenants.Queries.GetTenants;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = "SuperAdmin")]
public class TenantsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTenants(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTenantsQuery(search, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTenant(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetTenant), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateTenantCommand(
            id, request.Name, request.DatabaseMode,
            request.ConnectionStringEncrypted, request.CloudflareSubdomain, request.DbHost),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateTenant(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ToggleTenantStatusCommand(id, Activate: true), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateTenant(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ToggleTenantStatusCommand(id, Activate: false), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/users")]
    public async Task<IActionResult> GrantAccess(Guid id, [FromBody] GrantAccessRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new GrantTenantAccessCommand(id, request.MasterUserId, request.IsDefault, request.DisplayRole), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/users/{masterUserId:guid}")]
    public async Task<IActionResult> RevokeAccess(Guid id, Guid masterUserId, CancellationToken cancellationToken)
    {
        await sender.Send(new RevokeTenantAccessCommand(id, masterUserId), cancellationToken);
        return NoContent();
    }
}

public record UpdateTenantRequest(
    string Name,
    PhanMemKeToan.Domain.Enums.DatabaseMode DatabaseMode,
    string? ConnectionStringEncrypted,
    string? CloudflareSubdomain,
    string? DbHost);

public record GrantAccessRequest(
    Guid MasterUserId,
    bool IsDefault,
    string? DisplayRole);
