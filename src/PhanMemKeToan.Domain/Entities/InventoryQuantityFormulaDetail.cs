using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class InventoryQuantityFormulaDetail : AuditableEntity
{
    public Guid FormulaTemplateId { get; set; }
    public Guid MaterialItemId { get; set; }    // FK → InventoryItem
    public Guid UnitId { get; set; }
    public decimal Quantity { get; set; }       // precision (18,6)
    public int SortOrder { get; set; }

    // Navigation
    public InventoryQuantityFormulaTemplate FormulaTemplate { get; set; } = null!;
    public InventoryItem MaterialItem { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
}
