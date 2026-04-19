using MediatR;

namespace PhanMemKeToan.Application.Features.AccountObjects.Commands.UpdateAccountObject;

public record UpdateAccountObjectCommand(
    Guid Id,
    int RowVersion,
    string ObjectCode,
    string ObjectName,
    string? ObjectNameEnglish,
    string? Address,
    string? TaxCode,
    string? Email,
    string? Phone,
    string? Fax,
    string? Website,
    string? ContactPerson,
    string? ContactPhone,
    string? Description,
    int ObjectType,
    decimal CreditLimit,
    int PaymentTermDays,
    bool IsActive,
    Guid? AccountObjectGroupId,
    List<UpdateBankAccountDto> BankAccounts,
    List<UpdateOpeningBalanceDto> OpeningBalances,
    UpdateEmployeeProfileDto? EmployeeProfile,
    bool ConfirmRemoveProfile = false
) : IRequest<UpdateAccountObjectResult>;

public record UpdateBankAccountDto(
    Guid? Id,
    string BankName,
    string? BankBranch,
    string AccountNumber,
    string? SwiftCode
);

public record UpdateOpeningBalanceDto(
    Guid? Id,
    Guid CurrencyId,
    decimal DebitAmount,
    decimal CreditAmount,
    decimal DebitAmountOC,
    decimal CreditAmountOC,
    decimal ExchangeRate
);

public record UpdateEmployeeProfileDto(
    string? CitizenId,
    DateTime? DateOfBirth,
    int? Gender,
    string? SocialInsuranceNumber,
    DateTime? HireDate,
    Guid? DepartmentId,
    int DependentCount
);

public record UpdateAccountObjectResult(Guid Id, int RowVersion);
