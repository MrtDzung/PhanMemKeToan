using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Common.Models;
using PhanMemKeToan.Application.Features.InventoryItems.DTOs;

namespace PhanMemKeToan.Application.Features.InventoryItems.Queries.GetInventoryItems;

public class GetInventoryItemsQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<GetInventoryItemsQuery, PaginatedResult<InventoryItemListItemDto>>
{
    public async Task<PaginatedResult<InventoryItemListItemDto>> Handle(
        GetInventoryItemsQuery request, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var query = dbContext.InventoryItems
            .AsNoTracking()
            .Include(i => i.Unit)
            .Include(i => i.Category)
            .AsQueryable();

        // ItemType filter
        if (request.ItemType.HasValue)
            query = query.Where(i => (int)i.ItemType == request.ItemType.Value);

        // IsActive filter
        if (request.IsActive.HasValue)
            query = query.Where(i => i.IsActive == request.IsActive.Value);

        // CategoryId filter — include all descendants
        if (request.CategoryId.HasValue)
        {
            var descendantIds = await GetDescendantCategoryIds(request.CategoryId.Value, cancellationToken);
            query = query.Where(i => i.CategoryId.HasValue && descendantIds.Contains(i.CategoryId.Value));
        }

        // Search filter (ILIKE via EF Core ToLower)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.Trim().ToLower();
            query = query.Where(i =>
                i.ItemCode.ToLower().Contains(q) ||
                i.ItemName.ToLower().Contains(q));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var items = await query
            .OrderBy(i => i.ItemCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InventoryItemListItemDto
            {
                Id = i.Id,
                ItemCode = i.ItemCode,
                ItemName = i.ItemName,
                ItemType = (int)i.ItemType,
                UnitId = i.UnitId,
                UnitCode = i.Unit.UnitCode,
                CategoryId = i.CategoryId,
                CategoryName = i.Category != null ? i.Category.CategoryName : null,
                IsActive = i.IsActive,
                UnitPrice = i.UnitPrice,
                SalePrice1 = i.SalePrice1,
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<InventoryItemListItemDto>(items, totalCount, page, pageSize);
    }

    private async Task<HashSet<Guid>> GetDescendantCategoryIds(Guid rootId, CancellationToken cancellationToken)
    {
        var allCategories = await dbContext.InventoryItemCategories
            .AsNoTracking()
            .Select(c => new { c.Id, c.ParentId })
            .ToListAsync(cancellationToken);

        var result = new HashSet<Guid> { rootId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var child in allCategories.Where(c => c.ParentId == current))
            {
                result.Add(child.Id);
                queue.Enqueue(child.Id);
            }
        }

        return result;
    }
}
