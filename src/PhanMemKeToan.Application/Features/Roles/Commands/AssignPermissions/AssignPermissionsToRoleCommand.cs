using MediatR;

namespace PhanMemKeToan.Application.Features.Roles.Commands.AssignPermissions;

public record AssignPermissionsToRoleCommand(Guid RoleId, IReadOnlyList<Guid> PermissionIds) : IRequest;
