using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class AccountObject : AuditableEntity
{
    public string ObjectCode { get; set; } = string.Empty;         // max 25
    public string ObjectName { get; set; } = string.Empty;         // max 255
    public string? ObjectNameEnglish { get; set; }                 // max 255
    public string? Address { get; set; }                           // max 500
    public string? TaxCode { get; set; }                           // max 50
    public string? Email { get; set; }                             // max 255
    public string? Phone { get; set; }                             // max 50
    public string? Fax { get; set; }                               // max 50
    public string? Website { get; set; }                           // max 255
    public string? ContactPerson { get; set; }                     // max 255
    public string? ContactPhone { get; set; }                      // max 50
    public string? Description { get; set; }
    public int ObjectType { get; set; }                            // bitmask
    public decimal CreditLimit { get; set; }                       // precision (18,2)
    public int PaymentTermDays { get; set; }
    public bool IsActive { get; set; } = true;
    public int RowVersion { get; set; }                            // optimistic concurrency BR-DI02
    public Guid? AccountObjectGroupId { get; set; }

    // Navigation
    public AccountObjectGroup? AccountObjectGroup { get; set; }
    public ICollection<AccountObjectBankAccount> BankAccounts { get; set; } = [];
    public ICollection<AccountObjectOpeningBalance> OpeningBalances { get; set; } = [];
    public AccountObjectEmployeeProfile? EmployeeProfile { get; set; }
}
