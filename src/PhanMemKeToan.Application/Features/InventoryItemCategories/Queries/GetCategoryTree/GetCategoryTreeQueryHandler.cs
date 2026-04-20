using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.InventoryItems.DTOs;

namespace PhanMemKeToan.Application.Features.InventoryItemCategories.Queries.GetCategoryTree;

public class GetCategoryTreeQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<GetCategoryTreeQuery, List<CategoryTreeNodeDto>>
{
    public async Task<List<CategoryTreeNodeDto>> Handle(
        GetCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var all = await dbContext.InventoryItemCategories
            .AsNoTracking()
            .OrderBy(c => c.Level)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.CategoryCode)
            .Select(c => new CategoryTreeNodeDto
            {
                Id = c.Id,
                CategoryCode = c.CategoryCode,
                CategoryName = c.CategoryName,
                ParentId = c.ParentId,
                Level = c.Level,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder,
            })
            .ToListAsync(cancellationToken);

        var dict = all.ToDictionary(c => c.Id);

        foreach (var node in all)
        {
            if (node.ParentId.HasValue && dict.TryGetValue(node.ParentId.Value, out var parent))
                parent.Children.Add(node);
        }

        return all.Where(c => c.ParentId == null).ToList();
    }
}
