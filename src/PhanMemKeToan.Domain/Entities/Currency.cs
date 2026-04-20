using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class Currency : AuditableEntity
{
    public string CurrencyCode { get; set; } = string.Empty;       // max 10
    public string CurrencyName { get; set; } = string.Empty;       // max 100
    public string? CurrencyNameEnglish { get; set; }               // max 100
    public string Symbol { get; set; } = string.Empty;             // max 10
    public decimal ExchangeRate { get; set; }                      // precision (18,2)
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<AccountObjectOpeningBalance> OpeningBalances { get; set; } = [];
    public ICollection<InventoryItemOpeningBalance> InventoryOpeningBalances { get; set; } = [];
}
