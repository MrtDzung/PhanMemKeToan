using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.DeleteDepartment;

public class DeleteDepartmentCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<DeleteDepartmentCommand>
{
    public async Task Handle(DeleteDepartmentCommand cmd, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var entity = await dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == cmd.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Department), cmd.Id);

        var hasChildren = await dbContext.Departments
            .AnyAsync(d => d.ParentId == cmd.Id, cancellationToken);

        if (hasChildren)
            throw new BusinessRuleException("has_children", "Không thể xóa: bộ phận còn bộ phận con.");

        var hasEmployeeRefs = await dbContext.AccountObjectEmployeeProfiles
            .AnyAsync(x => x.DepartmentId == cmd.Id, cancellationToken);

        if (hasEmployeeRefs)
            throw new BusinessRuleException("has_references", "Không thể xóa: bộ phận đang được sử dụng.");

        entity.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
