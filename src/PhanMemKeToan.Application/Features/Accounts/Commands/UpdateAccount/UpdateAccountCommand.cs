using MediatR;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Accounts.Commands.UpdateAccount;

public record UpdateAccountCommand(
    Guid Id,
    int RowVersion,
    string AccountNumber,
    string AccountName,
    string? AccountNameEnglish,
    Guid? ParentId,
    AccountCategoryKind AccountCategoryKind,
    bool Inactive,
    bool IsPostableInForeignCurrency,
    bool DetailByAccountObject,
    AccountObjectType AccountObjectType,
    bool DetailByBankAccount,
    bool DetailByJob,
    bool DetailByProjectWork,
    bool DetailByOrder,
    bool DetailByContract,
    bool DetailByExpenseItem,
    bool DetailByDepartment,
    bool DetailByListItem,
    bool DetailByPUContract
) : IRequest<int>;
