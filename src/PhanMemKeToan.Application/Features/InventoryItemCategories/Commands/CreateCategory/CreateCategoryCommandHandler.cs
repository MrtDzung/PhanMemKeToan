using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.InventoryItemCategories.Commands.CreateCategory;

public class CreateCategoryCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        // Unique CategoryCode per tenant
        if (await dbContext.InventoryItemCategories.AnyAsync(c => c.CategoryCode == cmd.CategoryCode, cancellationToken))
            throw new ConflictException($"CategoryCode '{cmd.CategoryCode}' already exists.");

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

        var entity = new InventoryItemCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryCode = cmd.CategoryCode,
            CategoryName = cmd.CategoryName,
            ParentId = cmd.ParentId,
            Level = level,
            IsActive = cmd.IsActive,
            SortOrder = cmd.SortOrder,
        };

        dbContext.InventoryItemCategories.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
