using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Accounts.DTOs;

namespace PhanMemKeToan.Application.Features.Accounts.Queries.GetAccountTree;

public class GetAccountTreeQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IAccountCacheService cacheService)
    : IRequestHandler<GetAccountTreeQuery, object>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<object> Handle(GetAccountTreeQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        // Check Redis cache first — covers both tree and flat requests
        var cached = await cacheService.GetTreeAsync(tenantId, request.IncludeInactive);
        if (cached != null)
        {
            if (request.Format == "flat")
                return FlattenTree(cached);
            return cached;
        }

        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(a => request.IncludeInactive || !a.Inactive)
            .OrderBy(a => a.AccountNumber)
            .ToListAsync(cancellationToken);

        // Build tree in-memory from flat list
        var dtoMap = accounts.ToDictionary(
            a => a.Id,
            a => new AccountTreeNodeDto
            {
                AccountId = a.Id,
                AccountNumber = a.AccountNumber,
                AccountName = a.AccountName,
                AccountNameEnglish = a.AccountNameEnglish,
                ParentId = a.ParentID,
                Grade = a.Grade,
                IsParent = a.IsParent,
                AccountCategoryKind = a.AccountCategoryKind,
                Inactive = a.Inactive,
                IsPostableInForeignCurrency = a.IsPostableInForeignCurrency,
                HasTransactions = false // GL table not yet implemented
            });

        var roots = new List<AccountTreeNodeDto>();
        foreach (var acc in accounts)
        {
            var dto = dtoMap[acc.Id];
            if (acc.ParentID == null)
                roots.Add(dto);
            else if (dtoMap.TryGetValue(acc.ParentID.Value, out var parent))
                parent.Children.Add(dto);
        }

        await cacheService.SetTreeAsync(tenantId, request.IncludeInactive, roots, CacheTtl);

        if (request.Format == "flat")
            return FlattenTree(roots);

        return roots;
    }

    private static List<AccountListItemDto> FlattenTree(List<AccountTreeNodeDto> nodes)
    {
        var result = new List<AccountListItemDto>();
        void Traverse(List<AccountTreeNodeDto> items)
        {
            foreach (var n in items)
            {
                result.Add(new AccountListItemDto
                {
                    AccountId = n.AccountId,
                    AccountNumber = n.AccountNumber,
                    AccountName = n.AccountName,
                    ParentId = n.ParentId,
                    Grade = n.Grade,
                    AccountCategoryKind = n.AccountCategoryKind,
                    Inactive = n.Inactive,
                    IsParent = n.IsParent,
                    IsPostableInForeignCurrency = n.IsPostableInForeignCurrency
                });
                Traverse(n.Children);
            }
        }
        Traverse(nodes);
        return result;
    }
}
