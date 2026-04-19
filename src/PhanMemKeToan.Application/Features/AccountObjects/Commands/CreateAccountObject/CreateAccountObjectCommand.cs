using MediatR;

namespace PhanMemKeToan.Application.Features.AccountObjects.Commands.CreateAccountObject;

public record CreateAccountObjectCommand(
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
    List<CreateBankAccountDto> BankAccounts,
    List<CreateOpeningBalanceDto> OpeningBalances,
    CreateEmployeeProfileDto? EmployeeProfile
) : IRequest<CreateAccountObjectResult>;

public record CreateBankAccountDto(
    string BankName,
    string? BankBranch,
    string AccountNumber,
    string? SwiftCode
);

public record CreateOpeningBalanceDto(
    Guid CurrencyId,
    decimal DebitAmount,
    decimal CreditAmount,
    decimal DebitAmountOC,
    decimal CreditAmountOC,
    decimal ExchangeRate
);

public record CreateEmployeeProfileDto(
    string? CitizenId,
    DateTime? DateOfBirth,
    int? Gender,
    string? SocialInsuranceNumber,
    DateTime? HireDate,
    Guid? DepartmentId,
    int DependentCount
);

public record CreateAccountObjectResult(Guid Id, int RowVersion);
