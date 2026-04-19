using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.DeleteExpenseItem;

public class DeleteExpenseItemCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<DeleteExpenseItemCommand>
{
    public async Task Handle(DeleteExpenseItemCommand cmd, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var entity = await dbContext.ExpenseItems
            .FirstOrDefaultAsync(e => e.Id == cmd.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ExpenseItem), cmd.Id);

        entity.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
