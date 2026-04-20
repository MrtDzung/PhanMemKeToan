using MediatR;

namespace PhanMemKeToan.Application.Features.Roles.Commands.UpdateRole;

public record UpdateRoleCommand(Guid RoleId, string Name, string? Description) : IRequest;
