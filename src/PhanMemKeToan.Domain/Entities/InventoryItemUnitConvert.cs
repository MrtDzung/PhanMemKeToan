using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class InventoryItemUnitConvert : AuditableEntity
{
    public Guid InventoryItemId { get; set; }
    public Guid UnitId { get; set; }            // alternate/child unit
    public decimal ConvertRate { get; set; }    // precision (18,6) — how many main units = 1 child unit

    // Navigation
    public InventoryItem InventoryItem { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
}
