using MediatR;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.GrantTenantAccess;

public record GrantTenantAccessCommand(
    Guid TenantId,
    Guid MasterUserId,
    bool IsDefault,
    string? DisplayRole
) : IRequest;
