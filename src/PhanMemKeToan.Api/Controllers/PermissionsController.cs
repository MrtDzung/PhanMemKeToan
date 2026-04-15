using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.Permissions.Queries.GetPermissionMatrix;
using PhanMemKeToan.Application.Features.Permissions.Queries.GetPermissions;
using PhanMemKeToan.Application.Features.Roles.Commands.AssignPermissions;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "SYS.Permissions.View")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPermissionsQuery(), cancellationToken));

    [HttpGet("matrix")]
    [Authorize(Policy = "SYS.Permissions.View")]
    public async Task<IActionResult> GetMatrix(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPermissionMatrixQuery(), cancellationToken));

    [HttpPut("matrix")]
    [Authorize(Policy = "SYS.Permissions.Manage")]
    public async Task<IActionResult> UpdateMatrix([FromBody] UpdateMatrixRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new AssignPermissionsToRoleCommand(request.RoleId, request.PermissionIds), cancellationToken);
        return NoContent();
    }
}

public record UpdateMatrixRequest(Guid RoleId, IReadOnlyList<Guid> PermissionIds);
