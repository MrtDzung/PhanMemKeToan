using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common.Exceptions;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler(
    IApplicationDbContext dbContext,
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

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = request.Email,
            PasswordHash = passwordHasher.HashPassword(request.Password),
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

        await dbContext.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
