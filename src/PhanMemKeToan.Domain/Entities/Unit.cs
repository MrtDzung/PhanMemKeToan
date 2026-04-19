using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class Unit : AuditableEntity
{
    public string UnitCode { get; set; } = string.Empty;   // max 25
    public string UnitName { get; set; } = string.Empty;   // max 100
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<InventoryItem> Items { get; set; } = [];
    public ICollection<InventoryItemUnitConvert> UnitConverts { get; set; } = [];
    public ICollection<InventoryItemOpeningBalance> OpeningBalances { get; set; } = [];
    public ICollection<InventoryQuantityFormulaDetail> FormulaDetails { get; set; } = [];
}
