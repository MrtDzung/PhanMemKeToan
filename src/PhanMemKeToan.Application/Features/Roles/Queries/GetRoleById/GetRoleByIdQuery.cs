using MediatR;

namespace PhanMemKeToan.Application.Features.Roles.Queries.GetRoleById;

public record GetRoleByIdQuery(Guid RoleId) : IRequest<RoleDetailDto>;
public record RoleDetailDto(Guid Id, string Name, string? Description, IReadOnlyList<PermissionRefDto> Permissions);
public record PermissionRefDto(Guid Id, string Code, string Name, string ModuleCode);
