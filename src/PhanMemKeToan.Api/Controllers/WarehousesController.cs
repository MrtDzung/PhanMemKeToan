using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.Lookups.Commands.DeleteWarehouse;
using PhanMemKeToan.Application.Features.Lookups.Commands.UpsertWarehouse;
using PhanMemKeToan.Application.Features.Lookups.Queries.GetWarehouses;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize]
public class WarehousesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "DI.Warehouses.View")]
    public async Task<IActionResult> GetWarehouses([FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetWarehousesQuery(search), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpPost]
    [Authorize(Policy = "DI.Warehouses.Manage")]
    public async Task<IActionResult> UpsertWarehouse([FromBody] UpsertWarehouseCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DI.Warehouses.Manage")]
    public async Task<IActionResult> DeleteWarehouse(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteWarehouseCommand(id), cancellationToken);
        return NoContent();
    }
}
