using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Common.Interfaces;

public interface IMasterDbContext
{
    DbSet<MasterUser> MasterUsers { get; }
    DbSet<MasterUserTenant> MasterUserTenants { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
