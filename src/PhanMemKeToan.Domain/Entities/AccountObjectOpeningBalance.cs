using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class AccountObjectOpeningBalance : AuditableEntity
{
    public Guid AccountObjectId { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal DebitAmount { get; set; }                // precision (18,0)
    public decimal CreditAmount { get; set; }               // precision (18,0)
    public decimal DebitAmountOC { get; set; }              // precision (18,3) — original currency
    public decimal CreditAmountOC { get; set; }             // precision (18,3)
    public decimal ExchangeRate { get; set; } = 1;          // precision (18,2)

    // Navigation
    public AccountObject AccountObject { get; set; } = null!;
    public Currency Currency { get; set; } = null!;
}
