using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.UpdateTenant;

public class UpdateTenantCommandHandler(
    IMasterDbContext masterDbContext
) : IRequestHandler<UpdateTenantCommand>
{
    public async Task Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await masterDbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Tenant", request.Id);

        tenant.Name = request.Name;
        tenant.DatabaseMode = request.DatabaseMode;
        tenant.ConnectionStringEncrypted = request.ConnectionStringEncrypted;
        tenant.CloudflareSubdomain = request.CloudflareSubdomain;
        tenant.DbHost = request.DbHost;

        await masterDbContext.SaveChangesAsync(cancellationToken);
    }
}
