using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Lookups.DTOs;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertExpenseItem;

public class UpsertExpenseItemCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpsertExpenseItemCommand, ExpenseItemDto>
{
    public async Task<ExpenseItemDto> Handle(UpsertExpenseItemCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var codeExists = await dbContext.ExpenseItems.AnyAsync(
            e => e.TenantId == tenantId && e.ExpenseCode == cmd.ExpenseCode && e.Id != cmd.Id,
            cancellationToken);

        if (codeExists)
            throw new ConflictException($"ExpenseCode '{cmd.ExpenseCode}' đã tồn tại.");

        ExpenseItem entity;

        if (cmd.Id == null)
        {
            entity = new ExpenseItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ExpenseCode = cmd.ExpenseCode,
                ExpenseName = cmd.ExpenseName,
                IsActive = cmd.IsActive,
            };
            dbContext.ExpenseItems.Add(entity);
        }
        else
        {
            entity = await dbContext.ExpenseItems
                .FirstOrDefaultAsync(e => e.Id == cmd.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(ExpenseItem), cmd.Id);

            entity.ExpenseCode = cmd.ExpenseCode;
            entity.ExpenseName = cmd.ExpenseName;
            entity.IsActive = cmd.IsActive;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ExpenseItemDto(entity.Id, entity.ExpenseCode, entity.ExpenseName, entity.IsActive);
    }
}
