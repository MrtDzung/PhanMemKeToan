using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.CreateTenant;

public class CreateTenantCommandHandler(
    IMasterDbContext masterDbContext
) : IRequestHandler<CreateTenantCommand, Guid>
{
    public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var codeExists = await masterDbContext.Tenants
            .AnyAsync(t => t.Code == request.Code, cancellationToken);
        if (codeExists)
            throw new InvalidOperationException($"Tenant code '{request.Code}' already exists.");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            IsActive = true,
            DatabaseMode = request.DatabaseMode,
            DbStatus = request.DatabaseMode == DatabaseMode.OnPremise ? TenantDbStatus.Offline : TenantDbStatus.Online,
            ConnectionStringEncrypted = request.ConnectionStringEncrypted,
            CloudflareSubdomain = request.CloudflareSubdomain,
            DbHost = request.DbHost,
            CreatedAt = DateTimeOffset.UtcNow
        };

        masterDbContext.Tenants.Add(tenant);
        await masterDbContext.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }
}
