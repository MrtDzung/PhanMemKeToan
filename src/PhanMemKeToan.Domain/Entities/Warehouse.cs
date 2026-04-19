using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class Warehouse : AuditableEntity
{
    public string WarehouseCode { get; set; } = string.Empty;  // max 25
    public string WarehouseName { get; set; } = string.Empty;  // max 255
    public string? Address { get; set; }                       // max 500
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<InventoryItemOpeningBalance> OpeningBalances { get; set; } = [];
}
