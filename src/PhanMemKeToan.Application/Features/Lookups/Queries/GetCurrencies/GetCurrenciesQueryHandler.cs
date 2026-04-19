using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Queries.GetCurrencies;

public class GetCurrenciesQueryHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetCurrenciesQuery, List<CurrencyDto>>
{
    public async Task<List<CurrencyDto>> Handle(GetCurrenciesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        return await dbContext.Currencies
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.CurrencyCode)
            .Select(c => new CurrencyDto(
                c.Id,
                c.CurrencyCode,
                c.CurrencyName,
                c.CurrencyNameEnglish,
                c.Symbol,
                c.ExchangeRate,
                c.IsActive))
            .ToListAsync(cancellationToken);
    }
}
