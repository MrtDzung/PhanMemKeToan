using MediatR;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.UpdateTenant;

public record UpdateTenantCommand(
    Guid Id,
    string Name,
    DatabaseMode DatabaseMode,
    string? ConnectionStringEncrypted,
    string? CloudflareSubdomain,
    string? DbHost
) : IRequest;
