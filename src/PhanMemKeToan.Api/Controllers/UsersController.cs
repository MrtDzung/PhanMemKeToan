using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.Users.Commands.CreateUser;
using PhanMemKeToan.Application.Features.Users.Commands.ToggleUserActivation;
using PhanMemKeToan.Application.Features.Users.Commands.UnlockUser;
using PhanMemKeToan.Application.Features.Users.Commands.UpdateUser;
using PhanMemKeToan.Application.Features.Users.Queries.GetUserById;
using PhanMemKeToan.Application.Features.Users.Queries.GetUsers;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "SYS.Users.View")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetUsersQuery(search, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "SYS.Users.View")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "SYS.Users.Manage")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SYS.Users.Manage")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateUserCommand(id, request.FullName, request.Email, request.RoleIds), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "SYS.Users.Manage")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var currentUserIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (Guid.TryParse(currentUserIdStr, out var currentUserId) && currentUserId == id)
            return BadRequest(new { errorCode = "CANNOT_DEACTIVATE_SELF" });
        await sender.Send(new ToggleUserActivationCommand(id, Activate: false), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = "SYS.Users.Manage")]
    public async Task<IActionResult> ReactivateUser(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ToggleUserActivationCommand(id, Activate: true), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/unlock")]
    [Authorize(Policy = "SYS.Users.Manage")]
    public async Task<IActionResult> UnlockUser(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new UnlockUserCommand(id), cancellationToken);
        return NoContent();
    }
}

public record UpdateUserRequest(string FullName, string? Email, IReadOnlyList<Guid> RoleIds);
