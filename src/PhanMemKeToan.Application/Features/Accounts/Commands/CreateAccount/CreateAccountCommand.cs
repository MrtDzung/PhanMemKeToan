using MediatR;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Accounts.Commands.CreateAccount;

public record CreateAccountCommand(
    string AccountNumber,
    string AccountName,
    string? AccountNameEnglish,
    Guid? ParentId,
    AccountCategoryKind AccountCategoryKind,
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
) : IRequest<(Guid Id, int RowVersion)>;
