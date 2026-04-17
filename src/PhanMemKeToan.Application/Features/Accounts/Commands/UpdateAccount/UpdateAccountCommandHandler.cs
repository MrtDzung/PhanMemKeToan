using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Accounts.Commands.UpdateAccount;

public class UpdateAccountCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IAccountCacheService cacheService,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateAccountCommand, int>
{
    public async Task<int> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var account = await dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.Id);

        // Optimistic concurrency check
        if (account.RowVersion != request.RowVersion)
            throw new ConflictException($"Tài khoản đã bị thay đổi bởi người dùng khác. Vui lòng tải lại.");

        // Check if GL entries exist (simplified — GL table not implemented yet)
        bool hasTransactions = false;

        var codeChanged = account.AccountNumber != request.AccountNumber;
        var parentChanged = account.ParentID != request.ParentId;

        if (hasTransactions && (codeChanged || parentChanged))
            throw new LockedException("Không thể thay đổi mã tài khoản hoặc tài khoản cha khi đã có chứng từ liên quan.");

        // If code changed, validate uniqueness
        if (codeChanged && await dbContext.Accounts.AnyAsync(
            a => a.AccountNumber == request.AccountNumber && a.Id != request.Id, cancellationToken))
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("AccountNumber", $"Mã tài khoản '{request.AccountNumber}' đã tồn tại.")]);

        // If parent changed, validate prefix rule
        if (request.ParentId.HasValue)
        {
            var parent = await dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.ParentId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Account), request.ParentId.Value);

            if (!request.AccountNumber.StartsWith(parent.AccountNumber))
                throw new ValidationException([new FluentValidation.Results.ValidationFailure("AccountNumber", $"Mã tài khoản phải bắt đầu bằng mã tài khoản cha '{parent.AccountNumber}'.")
                {
                    ErrorCode = "code_must_start_with_parent"
                }]);

            account.Grade = parent.Grade + 1;
        }
        else
        {
            account.Grade = 1;
        }

        account.AccountNumber = request.AccountNumber;
        account.AccountName = request.AccountName;
        account.AccountNameEnglish = request.AccountNameEnglish;
        account.ParentID = request.ParentId;
        account.AccountCategoryKind = request.AccountCategoryKind;
        account.Inactive = request.Inactive;
        account.IsPostableInForeignCurrency = request.IsPostableInForeignCurrency;
        account.DetailByAccountObject = request.DetailByAccountObject;
        account.AccountObjectType = request.AccountObjectType;
        account.DetailByBankAccount = request.DetailByBankAccount;
        account.DetailByJob = request.DetailByJob;
        account.DetailByProjectWork = request.DetailByProjectWork;
        account.DetailByOrder = request.DetailByOrder;
        account.DetailByContract = request.DetailByContract;
        account.DetailByExpenseItem = request.DetailByExpenseItem;
        account.DetailByDepartment = request.DetailByDepartment;
        account.DetailByListItem = request.DetailByListItem;
        account.DetailByPUContract = request.DetailByPUContract;
        account.RowVersion++;
        account.ModifiedAt = DateTimeOffset.UtcNow;
        account.ModifiedBy = currentUserService.UserId ?? "system";

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateTreeAsync(tenantId);

        return account.RowVersion;
    }
}
