using PhanMemKeToan.Domain.Common;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Domain.Entities;

public class InventoryItem : AuditableEntity
{
    public string ItemCode { get; set; } = string.Empty;              // max 25
    public string ItemName { get; set; } = string.Empty;              // max 255
    public string? ItemNameEnglish { get; set; }                      // max 255
    public string? Description { get; set; }
    public string? Barcode { get; set; }                              // max 255
    public Guid UnitId { get; set; }                                  // primary/main unit
    public Guid? CategoryId { get; set; }
    public CostingMethod CostingMethod { get; set; } = CostingMethod.WeightedAverage;
    public InventoryItemType ItemType { get; set; } = InventoryItemType.Goods;
    public decimal? DefaultTaxRate { get; set; }                      // precision (5,2)
    public decimal? UnitPrice { get; set; }                           // precision (18,2)
    public decimal MinStockLevel { get; set; }                        // precision (18,2)
    public decimal MaxStockLevel { get; set; }                        // precision (18,2)
    public int LeadTimeDays { get; set; }
    public bool IsFollowSerial { get; set; }
    public bool IsFollowLot { get; set; }
    public bool IsFollowExpiry { get; set; }
    public bool IsPanelItem { get; set; }
    public Guid? PanelUnitId { get; set; }                            // DUAL FK — explicit config required
    public Guid? FormulaTemplateId { get; set; }
    public decimal? SalePrice1 { get; set; }                          // precision (18,2)
    public decimal? SalePrice2 { get; set; }                          // precision (18,2)
    public decimal? SalePrice3 { get; set; }                          // precision (18,2)
    public bool IsActive { get; set; } = true;
    public int RowVersion { get; set; }                               // optimistic concurrency

    // Navigation — DUAL FK to Unit (UnitId and PanelUnitId both point to Unit)
    public Unit Unit { get; set; } = null!;
    public Unit? PanelUnit { get; set; }

    // Navigation — other FKs
    public InventoryItemCategory? Category { get; set; }
    public InventoryQuantityFormulaTemplate? FormulaTemplate { get; set; }

    // Collections
    public ICollection<InventoryItemUnitConvert> UnitConverts { get; set; } = [];
    public ICollection<InventoryItemBarcode> Barcodes { get; set; } = [];
    public ICollection<InventoryItemAttribute> ItemAttributes { get; set; } = [];
    public ICollection<InventoryItemOpeningBalance> OpeningBalances { get; set; } = [];
}
