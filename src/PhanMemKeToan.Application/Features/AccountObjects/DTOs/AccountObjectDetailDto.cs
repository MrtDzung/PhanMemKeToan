namespace PhanMemKeToan.Application.Features.AccountObjects.DTOs;

public class AccountObjectDetailDto
{
    public Guid Id { get; set; }
    public string ObjectCode { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public string? ObjectNameEnglish { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Website { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? Description { get; set; }
    public int ObjectType { get; set; }
    public decimal CreditLimit { get; set; }
    public int PaymentTermDays { get; set; }
    public bool IsActive { get; set; }
    public int RowVersion { get; set; }
    public Guid? AccountObjectGroupId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<BankAccountDto> BankAccounts { get; set; } = [];
    public List<OpeningBalanceDto> OpeningBalances { get; set; } = [];
    public EmployeeProfileDto? EmployeeProfile { get; set; }
}
