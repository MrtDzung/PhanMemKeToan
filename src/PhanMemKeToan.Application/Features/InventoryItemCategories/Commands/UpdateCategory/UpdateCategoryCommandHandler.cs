using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.InventoryItemCategories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<UpdateCategoryCommand>
{
    public async Task Handle(UpdateCategoryCommand cmd, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var entity = await dbContext.InventoryItemCategories
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryItemCategory), cmd.Id);

        // Prevent self-reference
        if (cmd.ParentId.HasValue && cmd.ParentId.Value == cmd.Id)
            throw new BusinessRuleException("self_reference", "A category cannot be its own parent.");

        // Unique CategoryCode check (only if changed)
        if (!string.Equals(entity.CategoryCode, cmd.CategoryCode, StringComparison.OrdinalIgnoreCase))
        {
            if (await dbContext.InventoryItemCategories.AnyAsync(c => c.CategoryCode == cmd.CategoryCode, cancellationToken))
                throw new ConflictException($"CategoryCode '{cmd.CategoryCode}' already exists.");
        }

        int level = 1;

        if (cmd.ParentId.HasValue)
        {
            var parent = await dbContext.InventoryItemCategories
                .FirstOrDefaultAsync(c => c.Id == cmd.ParentId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(InventoryItemCategory), cmd.ParentId.Value);

            level = parent.Level + 1;

            if (level > 5)
                throw new BusinessRuleException("max_level_exceeded", "Category hierarchy cannot exceed 5 levels.");
        }

        entity.CategoryCode = cmd.CategoryCode;
        entity.CategoryName = cmd.CategoryName;
        entity.ParentId = cmd.ParentId;
        entity.Level = level;
        entity.IsActive = cmd.IsActive;
        entity.SortOrder = cmd.SortOrder;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
