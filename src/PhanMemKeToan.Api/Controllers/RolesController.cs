using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.Roles.Commands.CreateRole;
using PhanMemKeToan.Application.Features.Roles.Commands.DeleteRole;
using PhanMemKeToan.Application.Features.Roles.Commands.UpdateRole;
using PhanMemKeToan.Application.Features.Roles.Queries.GetRoleById;
using PhanMemKeToan.Application.Features.Roles.Queries.GetRoles;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "SYS.Roles.View")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRolesQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "SYS.Roles.View")]
    public async Task<IActionResult> GetRole(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRoleByIdQuery(id), cancellationToken));

    [HttpPost]
    [Authorize(Policy = "SYS.Roles.Manage")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetRole), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SYS.Roles.Manage")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateRoleCommand(id, request.Name, request.Description), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SYS.Roles.Manage")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteRoleCommand(id), cancellationToken);
        return NoContent();
    }
}

public record UpdateRoleRequest(string Name, string? Description);
