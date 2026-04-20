using MediatR;

namespace PhanMemKeToan.Application.Features.Roles.Commands.DeleteRole;

public record DeleteRoleCommand(Guid RoleId) : IRequest;
