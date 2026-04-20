using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role", request.RoleId);

        if (role.Name != request.Name && await dbContext.Roles.AnyAsync(r => r.Name == request.Name && r.Id != request.RoleId, cancellationToken))
            throw new InvalidOperationException($"Role '{request.Name}' already exists.");

        role.Name = request.Name;
        role.Description = request.Description;
        role.ModifiedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
