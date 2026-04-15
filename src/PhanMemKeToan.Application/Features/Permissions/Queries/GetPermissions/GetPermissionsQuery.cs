using MediatR;

namespace PhanMemKeToan.Application.Features.Permissions.Queries.GetPermissions;

public record GetPermissionsQuery : IRequest<IReadOnlyList<PermissionGroupDto>>;
public record PermissionGroupDto(string ModuleCode, IReadOnlyList<PermissionItemDto> Permissions);
public record PermissionItemDto(Guid Id, string Code, string Name);
