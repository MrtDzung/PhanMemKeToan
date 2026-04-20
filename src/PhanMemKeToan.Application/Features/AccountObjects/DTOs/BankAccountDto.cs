namespace PhanMemKeToan.Application.Features.AccountObjects.DTOs;

public class BankAccountDto
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BankBranch { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string? SwiftCode { get; set; }
}
