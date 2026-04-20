using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.Auth.Queries.GetCurrentUser;
using PhanMemKeToan.Application.Features.Users.Commands.ChangePassword;
using PhanMemKeToan.Application.Features.Users.Commands.UpdateProfile;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(ISender sender) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentUserQuery(CurrentUserId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateProfileCommand(CurrentUserId, request.FullName), cancellationToken);
        return NoContent();
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangePasswordCommand(CurrentUserId, request.CurrentPassword, request.NewPassword), cancellationToken);
        return NoContent();
    }
}

public record UpdateProfileRequest(string FullName);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
