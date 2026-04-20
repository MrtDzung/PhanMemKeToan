using MediatR;

namespace PhanMemKeToan.Application.Features.Roles.Queries.GetRoles;

public record GetRolesQuery : IRequest<IReadOnlyList<RoleListDto>>;

public record RoleListDto(Guid Id, string Name, string? Description, int UserCount, int PermissionCount);
