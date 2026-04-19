using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class AccountObjectBankAccount : AuditableEntity
{
    public Guid AccountObjectId { get; set; }
    public string BankName { get; set; } = string.Empty;    // max 255
    public string? BankBranch { get; set; }                 // max 255
    public string AccountNumber { get; set; } = string.Empty; // max 50
    public string? SwiftCode { get; set; }                  // max 20

    // Navigation
    public AccountObject AccountObject { get; set; } = null!;
}
