using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Accounts.Commands.CreateAccount;

public class CreateAccountCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IAccountCacheService cacheService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateAccountCommand, (Guid Id, int RowVersion)>
{
    public async Task<(Guid Id, int RowVersion)> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        // Check uniqueness
        if (await dbContext.Accounts.AnyAsync(a => a.AccountNumber == request.AccountNumber, cancellationToken))
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("AccountNumber", $"Mã tài khoản '{request.AccountNumber}' đã tồn tại.")
            {
                ErrorCode = "duplicate_code"
            }]);

        int grade = 1;
        Account? parent = null;

        if (request.ParentId.HasValue)
        {
            parent = await dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.ParentId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Account), request.ParentId.Value);

            // Validate prefix rule (BR-DI01)
            if (!request.AccountNumber.StartsWith(parent.AccountNumber))
                throw new ValidationException([new FluentValidation.Results.ValidationFailure("AccountNumber", $"Mã tài khoản phải bắt đầu bằng mã tài khoản cha '{parent.AccountNumber}'.")
                {
                    ErrorCode = "code_must_start_with_parent"
                }]);

            grade = parent.Grade + 1;
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountNumber = request.AccountNumber,
            AccountName = request.AccountName,
            AccountNameEnglish = request.AccountNameEnglish,
            ParentID = request.ParentId,
            Grade = grade,
            IsParent = false,
            AccountCategoryKind = request.AccountCategoryKind,
            IsPostableInForeignCurrency = request.IsPostableInForeignCurrency,
            DetailByAccountObject = request.DetailByAccountObject,
            AccountObjectType = request.AccountObjectType,
            DetailByBankAccount = request.DetailByBankAccount,
            DetailByJob = request.DetailByJob,
            DetailByProjectWork = request.DetailByProjectWork,
            DetailByOrder = request.DetailByOrder,
            DetailByContract = request.DetailByContract,
            DetailByExpenseItem = request.DetailByExpenseItem,
            DetailByDepartment = request.DetailByDepartment,
            DetailByListItem = request.DetailByListItem,
            DetailByPUContract = request.DetailByPUContract,
            RowVersion = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUserService.UserId ?? "system"
        };

        dbContext.Accounts.Add(account);

        // Auto-set parent.IsParent = true
        if (parent != null && !parent.IsParent)
        {
            parent.IsParent = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateTreeAsync(tenantId);

        return (account.Id, account.RowVersion);
    }
}
