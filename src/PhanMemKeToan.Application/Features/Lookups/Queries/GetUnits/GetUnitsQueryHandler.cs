using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Queries.GetUnits;

public class GetUnitsQueryHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetUnitsQuery, List<UnitDto>>
{
    public async Task<List<UnitDto>> Handle(GetUnitsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var query = dbContext.Units
            .Where(u => u.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(u => u.UnitCode.ToLower().Contains(search) || u.UnitName.ToLower().Contains(search));
        }

        return await query
            .OrderBy(u => u.UnitCode)
            .Select(u => new UnitDto(u.Id, u.UnitCode, u.UnitName, u.IsActive))
            .ToListAsync(cancellationToken);
    }
}
