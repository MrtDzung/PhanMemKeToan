using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Accounts.Commands.DeleteAccount;

public class DeleteAccountCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IAccountCacheService cacheService,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteAccountCommand>
{
    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var account = await dbContext.Accounts
            .Include(a => a.Children)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.Id);

        // Optimistic concurrency check
        if (account.RowVersion != request.RowVersion)
            throw new ConflictException("Tài khoản đã bị thay đổi bởi người dùng khác. Vui lòng tải lại.");

        // Cannot delete if has children
        if (account.Children.Any(c => !c.IsDeleted))
            throw new BusinessRuleException("has_children", "Không thể xóa tài khoản vì còn tài khoản con.");

        // Cannot delete if has GL transactions
        // Note: For now, as GL balances are not fully implemented, we assume no transactions unless marked otherwise.
        // Once implemented, replace this with actual check.
        // if (await CheckHasTransactionsAsync(account.Id, cancellationToken))
        //     throw new BusinessRuleException("has_transactions", "Không thể xóa tài khoản vì đã phát sinh giao dịch.");

        // Soft-delete
        account.IsDeleted = true;
        account.Inactive = true;
        account.ModifiedAt = DateTimeOffset.UtcNow;
        account.ModifiedBy = currentUserService.UserId ?? "system";

        // If parent has no remaining active children, set IsParent = false
        if (account.ParentID.HasValue)
        {
            var remainingChildren = await dbContext.Accounts
                .AnyAsync(a => a.ParentID == account.ParentID && a.Id != account.Id, cancellationToken);

            if (!remainingChildren)
            {
                var parent = await dbContext.Accounts
                    .FirstOrDefaultAsync(a => a.Id == account.ParentID.Value, cancellationToken);
                if (parent != null)
                    parent.IsParent = false;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateTreeAsync(tenantId);
    }
}
