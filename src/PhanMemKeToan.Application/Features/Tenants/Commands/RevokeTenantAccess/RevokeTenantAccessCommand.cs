using MediatR;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.RevokeTenantAccess;

public record RevokeTenantAccessCommand(
    Guid TenantId,
    Guid MasterUserId
) : IRequest;
