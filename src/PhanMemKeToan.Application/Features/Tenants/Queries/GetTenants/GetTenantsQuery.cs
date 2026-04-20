using MediatR;
using PhanMemKeToan.Application.Common.Models;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Tenants.Queries.GetTenants;

public record GetTenantsQuery(
    string? Search,
    int Page = 1,
    int PageSize = 20
) : IRequest<PaginatedResult<TenantListDto>>;

public record TenantListDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    DatabaseMode DatabaseMode,
    TenantDbStatus DbStatus,
    int UserCount,
    DateTimeOffset CreatedAt
);
