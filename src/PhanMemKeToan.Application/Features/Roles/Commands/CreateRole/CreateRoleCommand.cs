using MediatR;

namespace PhanMemKeToan.Application.Features.Roles.Commands.CreateRole;

public record CreateRoleCommand(string Name, string? Description, IReadOnlyList<Guid> PermissionIds) : IRequest<Guid>;
