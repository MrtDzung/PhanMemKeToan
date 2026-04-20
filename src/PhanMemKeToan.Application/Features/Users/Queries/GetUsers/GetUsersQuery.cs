using MediatR;
using PhanMemKeToan.Application.Common.Models;

namespace PhanMemKeToan.Application.Features.Users.Queries.GetUsers;

public record GetUsersQuery(
    string? Search,
    int Page = 1,
    int PageSize = 20
) : IRequest<PaginatedResult<UserListDto>>;

public record UserListDto(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    bool IsLocked,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles
);
