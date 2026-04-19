using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Queries.GetExpenseItems;

public class GetExpenseItemsQueryHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetExpenseItemsQuery, List<ExpenseItemDto>>
{
    public async Task<List<ExpenseItemDto>> Handle(GetExpenseItemsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var query = dbContext.ExpenseItems
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(e => e.ExpenseCode.ToLower().Contains(search) || e.ExpenseName.ToLower().Contains(search));
        }

        return await query
            .OrderBy(e => e.ExpenseCode)
            .Select(e => new ExpenseItemDto(e.Id, e.ExpenseCode, e.ExpenseName, e.IsActive))
            .ToListAsync(cancellationToken);
    }
}
