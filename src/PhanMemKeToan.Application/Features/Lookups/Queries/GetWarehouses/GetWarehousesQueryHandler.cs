using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Queries.GetWarehouses;

public class GetWarehousesQueryHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetWarehousesQuery, List<WarehouseDto>>
{
    public async Task<List<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var query = dbContext.Warehouses
            .Where(w => w.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(w => w.WarehouseCode.ToLower().Contains(search) || w.WarehouseName.ToLower().Contains(search));
        }

        return await query
            .OrderBy(w => w.WarehouseCode)
            .Select(w => new WarehouseDto(w.Id, w.WarehouseCode, w.WarehouseName, w.Address, w.IsActive))
            .ToListAsync(cancellationToken);
    }
}
