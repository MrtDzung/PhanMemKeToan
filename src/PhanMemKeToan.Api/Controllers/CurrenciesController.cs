using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.Lookups.Commands.DeleteCurrency;
using PhanMemKeToan.Application.Features.Lookups.Commands.UpsertCurrency;
using PhanMemKeToan.Application.Features.Lookups.Queries.GetCurrencies;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/currencies")]
[Authorize]
public class CurrenciesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "DI.Currencies.View")]
    public async Task<IActionResult> GetCurrencies(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrenciesQuery(), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpPost]
    [Authorize(Policy = "DI.Currencies.Manage")]
    public async Task<IActionResult> UpsertCurrency([FromBody] UpsertCurrencyCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DI.Currencies.Manage")]
    public async Task<IActionResult> DeleteCurrency(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCurrencyCommand(id), cancellationToken);
        return NoContent();
    }
}
