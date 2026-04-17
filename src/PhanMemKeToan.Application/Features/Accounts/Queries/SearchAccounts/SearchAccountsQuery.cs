using MediatR;
using PhanMemKeToan.Application.Features.Accounts.DTOs;

namespace PhanMemKeToan.Application.Features.Accounts.Queries.SearchAccounts;

public record SearchAccountsQuery(string Q, bool PostableOnly = true, int Limit = 20) : IRequest<List<AccountListItemDto>>;
