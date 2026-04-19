using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.Lookups.Commands.DeleteExpenseItem;
using PhanMemKeToan.Application.Features.Lookups.Commands.UpsertExpenseItem;
using PhanMemKeToan.Application.Features.Lookups.Queries.GetExpenseItems;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/expense-items")]
[Authorize]
public class ExpenseItemsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "DI.ExpenseItems.View")]
    public async Task<IActionResult> GetExpenseItems([FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetExpenseItemsQuery(search), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpPost]
    [Authorize(Policy = "DI.ExpenseItems.Manage")]
    public async Task<IActionResult> UpsertExpenseItem([FromBody] UpsertExpenseItemCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DI.ExpenseItems.Manage")]
    public async Task<IActionResult> DeleteExpenseItem(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteExpenseItemCommand(id), cancellationToken);
        return NoContent();
    }
}
