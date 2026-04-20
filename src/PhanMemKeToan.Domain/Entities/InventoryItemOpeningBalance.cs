using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

/// <summary>
/// DD-006 — Opening stock balance per warehouse.
/// Service items (ItemType = Service = 3) must never have rows here (BR-IN05).
/// </summary>
public class InventoryItemOpeningBalance : AuditableEntity
{
    public Guid InventoryItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid UnitId { get; set; }
    public decimal Quantity { get; set; }           // precision (18,6)
    public decimal UnitCost { get; set; }           // precision (18,2)
    public decimal Amount { get; set; }             // precision (18,0) — Qty × UnitCost, rounded VND
    public Guid? CurrencyId { get; set; }
    public decimal? ForeignAmount { get; set; }     // precision (18,3)
    public decimal ExchangeRate { get; set; } = 1;  // precision (18,2)

    // Navigation
    public InventoryItem InventoryItem { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
    public Currency? Currency { get; set; }
}
