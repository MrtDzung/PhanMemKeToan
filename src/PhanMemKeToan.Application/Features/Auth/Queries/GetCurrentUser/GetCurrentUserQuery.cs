using MediatR;
using PhanMemKeToan.Application.Common.Models;

namespace PhanMemKeToan.Application.Features.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserDto>;

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    Guid TenantId,
    string TenantName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CompanyInfo> Companies,
    CompanyInfo? CurrentCompany
);
