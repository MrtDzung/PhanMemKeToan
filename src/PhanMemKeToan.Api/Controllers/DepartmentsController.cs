using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.Lookups.Commands.DeleteDepartment;
using PhanMemKeToan.Application.Features.Lookups.Commands.UpsertDepartment;
using PhanMemKeToan.Application.Features.Lookups.Queries.GetDepartments;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "DI.Departments.View")]
    public async Task<IActionResult> GetDepartments([FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetDepartmentsQuery(search), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpPost]
    [Authorize(Policy = "DI.Departments.Manage")]
    public async Task<IActionResult> UpsertDepartment([FromBody] UpsertDepartmentCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DI.Departments.Manage")]
    public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteDepartmentCommand(id), cancellationToken);
        return NoContent();
    }
}
