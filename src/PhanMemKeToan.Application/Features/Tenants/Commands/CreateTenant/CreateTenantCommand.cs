using MediatR;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.CreateTenant;

public record CreateTenantCommand(
    string Code,
    string Name,
    DatabaseMode DatabaseMode = DatabaseMode.CloudManaged,
    string? ConnectionStringEncrypted = null,
    string? CloudflareSubdomain = null,
    string? DbHost = null
) : IRequest<Guid>;
