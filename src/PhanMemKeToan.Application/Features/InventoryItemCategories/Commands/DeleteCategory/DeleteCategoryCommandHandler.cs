using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.InventoryItemCategories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand cmd, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var entity = await dbContext.InventoryItemCategories
            .Include(c => c.Children)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryItemCategory), cmd.Id);

        if (entity.Children.Count > 0)
            throw new BusinessRuleException("has_children", "Cannot delete category that has sub-categories.");

        if (entity.Items.Count > 0)
            throw new BusinessRuleException("has_items", "Cannot delete category that has inventory items assigned.");

        entity.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
