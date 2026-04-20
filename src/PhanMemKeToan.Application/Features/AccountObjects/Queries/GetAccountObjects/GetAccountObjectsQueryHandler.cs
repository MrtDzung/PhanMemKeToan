using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Common.Models;
using PhanMemKeToan.Application.Features.AccountObjects.DTOs;

namespace PhanMemKeToan.Application.Features.AccountObjects.Queries.GetAccountObjects;

public class GetAccountObjectsQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<GetAccountObjectsQuery, PaginatedResult<AccountObjectListItemDto>>
{
    public async Task<PaginatedResult<AccountObjectListItemDto>> Handle(
        GetAccountObjectsQuery request, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var query = dbContext.AccountObjects.AsNoTracking();

        // Type filter (bitmask)
        if (request.TypeFilter > 0)
            query = query.Where(e => (e.ObjectType & request.TypeFilter) != 0);

        // Status filter
        if (request.Status == "active")
            query = query.Where(e => e.IsActive);
        else if (request.Status == "inactive")
            query = query.Where(e => !e.IsActive);

        // Search filter (case-insensitive via ToLower — translates to lower() in PostgreSQL)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.Trim().ToLower();
            query = query.Where(e =>
                e.ObjectCode.ToLower().Contains(q) ||
                e.ObjectName.ToLower().Contains(q));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sort
        query = (request.SortBy.ToLowerInvariant(), request.SortDir.ToLowerInvariant()) switch
        {
            ("objectname", "desc") => query.OrderByDescending(e => e.ObjectName),
            ("objectname", _)      => query.OrderBy(e => e.ObjectName),
            ("createdat", "desc")  => query.OrderByDescending(e => e.CreatedAt),
            ("createdat", _)       => query.OrderBy(e => e.CreatedAt),
            (_, "desc")            => query.OrderByDescending(e => e.ObjectCode),
            _                      => query.OrderBy(e => e.ObjectCode),
        };

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AccountObjectListItemDto
            {
                Id = e.Id,
                ObjectCode = e.ObjectCode,
                ObjectName = e.ObjectName,
                ObjectType = e.ObjectType,
                TaxCode = e.TaxCode,
                Phone = e.Phone,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<AccountObjectListItemDto>(items, totalCount, page, pageSize);
    }
}
