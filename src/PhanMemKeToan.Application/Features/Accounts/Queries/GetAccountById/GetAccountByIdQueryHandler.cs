using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Accounts.DTOs;

namespace PhanMemKeToan.Application.Features.Accounts.Queries.GetAccountById;

public class GetAccountByIdQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<GetAccountByIdQuery, AccountDetailDto>
{
    public async Task<AccountDetailDto> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var account = await dbContext.Accounts
            .AsNoTracking()
            .Include(a => a.Parent)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Account), request.Id);

        return new AccountDetailDto
        {
            AccountId = account.Id,
            AccountNumber = account.AccountNumber,
            AccountName = account.AccountName,
            AccountNameEnglish = account.AccountNameEnglish,
            Grade = account.Grade,
            IsParent = account.IsParent,
            AccountCategoryKind = account.AccountCategoryKind,
            Inactive = account.Inactive,
            IsPostableInForeignCurrency = account.IsPostableInForeignCurrency,
            HasTransactions = false, // GL table not yet implemented
            ParentId = account.ParentID,
            ParentNumber = account.Parent?.AccountNumber,
            ParentName = account.Parent?.AccountName,
            DetailByAccountObject = account.DetailByAccountObject,
            AccountObjectType = account.AccountObjectType,
            DetailByBankAccount = account.DetailByBankAccount,
            DetailByJob = account.DetailByJob,
            DetailByProjectWork = account.DetailByProjectWork,
            DetailByOrder = account.DetailByOrder,
            DetailByContract = account.DetailByContract,
            DetailByExpenseItem = account.DetailByExpenseItem,
            DetailByDepartment = account.DetailByDepartment,
            DetailByListItem = account.DetailByListItem,
            DetailByPUContract = account.DetailByPUContract,
            RowVersion = account.RowVersion,
            CreatedAt = account.CreatedAt,
            CreatedBy = account.CreatedBy,
            ModifiedAt = account.ModifiedAt,
            ModifiedBy = account.ModifiedBy
        };
    }
}
