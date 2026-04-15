using MediatR;

namespace PhanMemKeToan.Application.Features.Users.Queries.GetUserById;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDetailDto>;

public record UserDetailDto(
    Guid Id, string Email, string FullName, bool IsActive, bool IsLocked,
    DateTimeOffset? LastLoginAt, DateTimeOffset CreatedAt,
    IReadOnlyList<RoleRefDto> Roles
);
public record RoleRefDto(Guid Id, string Name);
