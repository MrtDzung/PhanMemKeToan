using MediatR;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Tenants.Queries.GetTenantById;

public record GetTenantByIdQuery(Guid Id) : IRequest<TenantDetailDto>;

public record TenantDetailDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    DatabaseMode DatabaseMode,
    TenantDbStatus DbStatus,
    string? DatabaseSchemaName,
    string? CloudflareSubdomain,
    string? DbHost,
    bool HasConnectionString,
    int UserCount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TenantUserDto> Users
);

public record TenantUserDto(
    Guid UserId,
    string Email,
    string FullName,
    string? DisplayRole,
    bool IsDefault,
    DateTimeOffset JoinedAt
);
