using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Accounts.DTOs;

namespace PhanMemKeToan.Application.Features.Accounts.Queries.SearchAccounts;

public class SearchAccountsQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<SearchAccountsQuery, List<AccountListItemDto>>
{
    public async Task<List<AccountListItemDto>> Handle(SearchAccountsQuery request, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        if (string.IsNullOrWhiteSpace(request.Q))
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("Q", "Search query is required.")]);

        var limit = Math.Min(request.Limit, 50);
        var q = request.Q.Trim();

        var query = dbContext.Accounts
            .AsNoTracking()
            .Where(a => !a.Inactive);

        if (request.PostableOnly)
            query = query.Where(a => !a.IsParent);

        // Prefix match on AccountNumber OR name contains
        var results = await query
            .Where(a => EF.Functions.Like(a.AccountNumber, q + "%") || a.AccountName.Contains(q))
            .OrderBy(a => a.AccountNumber.StartsWith(q) ? 0 : 1)
            .ThenBy(a => a.AccountNumber)
            .Take(limit)
            .Select(a => new AccountListItemDto
            {
                AccountId = a.Id,
                AccountNumber = a.AccountNumber,
                AccountName = a.AccountName,
                AccountCategoryKind = a.AccountCategoryKind,
                Inactive = a.Inactive,
                IsParent = a.IsParent
            })
            .ToListAsync(cancellationToken);

        return results;
    }
}
