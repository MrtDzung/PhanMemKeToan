using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common.Exceptions;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler(
    IApplicationDbContext dbContext,
    IMasterDbContext masterDbContext,
    IPasswordHasher passwordHasher,
    ITenantContext tenantContext
) : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        // Check email uniqueness within tenant
        var emailExists = await dbContext.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);
        if (emailExists)
            throw new DuplicateEmailException(request.Email);

        // Validate roles belong to this tenant
        if (request.RoleIds.Any())
        {
            var validRoleCount = await dbContext.Roles
                .CountAsync(r => request.RoleIds.Contains(r.Id), cancellationToken);
            if (validRoleCount != request.RoleIds.Count)
                throw new NotFoundException("Role", "one or more roleIds");
        }

        var passwordHash = passwordHasher.HashPassword(request.Password);
        var userId = Guid.NewGuid();

        // Create User in Tenant DB
        var user = new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = request.Email,
            PasswordHash = passwordHash,
            FullName = request.FullName,
            IsActive = true,
            FailedLoginCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "system"
        };

        dbContext.Users.Add(user);

        foreach (var roleId in request.RoleIds)
        {
            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        // Create or link MasterUser in Master DB
        var existingMasterUser = await masterDbContext.MasterUsers
            .FirstOrDefaultAsync(mu => mu.Email == request.Email, cancellationToken);

        if (existingMasterUser is null)
        {
            // New user across all tenants — create MasterUser
            var masterUser = new MasterUser
            {
                Id = userId,
                Email = request.Email,
                PasswordHash = passwordHash,
                FullName = request.FullName,
                IsActive = true,
                FailedLoginCount = 0,
                CreatedAt = DateTimeOffset.UtcNow
            };
            masterDbContext.MasterUsers.Add(masterUser);
            masterDbContext.MasterUserTenants.Add(new MasterUserTenant
            {
                MasterUserId = userId,
                TenantId = tenantId,
                IsDefault = true,
                JoinedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            // User exists in another tenant — link to this tenant
            // Use the existing MasterUser's Id for cross-DB identity
            user.Id = existingMasterUser.Id;
            masterDbContext.MasterUserTenants.Add(new MasterUserTenant
            {
                MasterUserId = existingMasterUser.Id,
                TenantId = tenantId,
                IsDefault = false,
                JoinedAt = DateTimeOffset.UtcNow
            });
        }

        await masterDbContext.SaveChangesAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
