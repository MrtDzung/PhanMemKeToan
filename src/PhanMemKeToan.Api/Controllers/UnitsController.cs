using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.Lookups.Commands.DeleteUnit;
using PhanMemKeToan.Application.Features.Lookups.Commands.UpsertUnit;
using PhanMemKeToan.Application.Features.Lookups.Queries.GetUnits;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/units")]
[Authorize]
public class UnitsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "DI.Units.View")]
    public async Task<IActionResult> GetUnits([FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetUnitsQuery(search), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpPost]
    [Authorize(Policy = "DI.Units.Manage")]
    public async Task<IActionResult> UpsertUnit([FromBody] UpsertUnitCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DI.Units.Manage")]
    public async Task<IActionResult> DeleteUnit(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteUnitCommand(id), cancellationToken);
        return NoContent();
    }
}
