using MediatR;
using PhanMemKeToan.Application.Common.Models;
using PhanMemKeToan.Application.Features.AccountObjects.DTOs;

namespace PhanMemKeToan.Application.Features.AccountObjects.Queries.GetAccountObjects;

public record GetAccountObjectsQuery(
    int Page = 1,
    int PageSize = 25,
    int TypeFilter = 0,
    string Status = "all",
    string? Search = null,
    string SortBy = "objectCode",
    string SortDir = "asc"
) : IRequest<PaginatedResult<AccountObjectListItemDto>>;
