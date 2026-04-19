using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.DeleteUnit;

public class DeleteUnitCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<DeleteUnitCommand>
{
    public async Task Handle(DeleteUnitCommand cmd, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var entity = await dbContext.Units
            .FirstOrDefaultAsync(u => u.Id == cmd.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Unit), cmd.Id);

        var refItems = await dbContext.InventoryItems
            .AnyAsync(x => x.UnitId == cmd.Id, cancellationToken);

        var refConverts = await dbContext.InventoryItemUnitConverts
            .AnyAsync(x => x.UnitId == cmd.Id, cancellationToken);

        var refFormulas = await dbContext.InventoryQuantityFormulaDetails
            .AnyAsync(x => x.UnitId == cmd.Id, cancellationToken);

        var refOpeningBalances = await dbContext.InventoryItemOpeningBalances
            .AnyAsync(x => x.UnitId == cmd.Id, cancellationToken);

        if (refItems || refConverts || refFormulas || refOpeningBalances)
            throw new BusinessRuleException("has_references", "Không thể xóa: đơn vị tính đang được sử dụng.");

        entity.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
