using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.DeleteWarehouse;

public class DeleteWarehouseCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<DeleteWarehouseCommand>
{
    public async Task Handle(DeleteWarehouseCommand cmd, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var entity = await dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == cmd.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Warehouse), cmd.Id);

        var hasRefs = await dbContext.InventoryItemOpeningBalances
            .AnyAsync(x => x.WarehouseId == cmd.Id, cancellationToken);

        if (hasRefs)
            throw new BusinessRuleException("has_references", "Không thể xóa: kho đang được sử dụng.");

        entity.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
