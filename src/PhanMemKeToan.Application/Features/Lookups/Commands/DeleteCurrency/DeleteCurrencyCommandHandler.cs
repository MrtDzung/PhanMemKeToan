using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.DeleteCurrency;

public class DeleteCurrencyCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<DeleteCurrencyCommand>
{
    public async Task Handle(DeleteCurrencyCommand cmd, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var entity = await dbContext.Currencies
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Currency), cmd.Id);

        var hasRefs = await dbContext.AccountObjectOpeningBalances
            .AnyAsync(x => x.CurrencyId == cmd.Id, cancellationToken);

        if (hasRefs)
            throw new BusinessRuleException("has_references", "Không thể xóa: tiền tệ đang được sử dụng.");

        entity.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
