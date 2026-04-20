using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Common.Models;

namespace PhanMemKeToan.Application.Features.Tenants.Queries.GetTenants;

public class GetTenantsQueryHandler(
    IMasterDbContext masterDbContext
) : IRequestHandler<GetTenantsQuery, PaginatedResult<TenantListDto>>
{
    public async Task<PaginatedResult<TenantListDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var query = masterDbContext.Tenants.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(search) || t.Code.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TenantListDto(
                t.Id,
                t.Code,
                t.Name,
                t.IsActive,
                t.DatabaseMode,
                t.DbStatus,
                t.MasterUserTenants.Count,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<TenantListDto>(items, totalCount, request.Page, request.PageSize);
    }
}
