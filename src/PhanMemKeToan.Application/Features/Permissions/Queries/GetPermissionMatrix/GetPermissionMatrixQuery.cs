using MediatR;

namespace PhanMemKeToan.Application.Features.Permissions.Queries.GetPermissionMatrix;

public record GetPermissionMatrixQuery : IRequest<PermissionMatrixDto>;

public record PermissionMatrixDto(
    IReadOnlyList<RoleColumnDto> Roles,
    IReadOnlyList<PermissionModuleDto> Modules
);
public record RoleColumnDto(Guid Id, string Name);
public record PermissionModuleDto(string ModuleCode, IReadOnlyList<PermissionRowDto> Permissions);
public record PermissionRowDto(Guid Id, string Code, string Name, IReadOnlyDictionary<Guid, bool> RoleAssignments);
