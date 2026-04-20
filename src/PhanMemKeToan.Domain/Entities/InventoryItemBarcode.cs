using PhanMemKeToan.Domain.Common;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Domain.Entities;

public class InventoryItemBarcode : AuditableEntity
{
    public Guid InventoryItemId { get; set; }
    public string BarcodeValue { get; set; } = string.Empty;  // max 255
    public BarcodeType BarcodeType { get; set; }

    // Navigation
    public InventoryItem InventoryItem { get; set; } = null!;
}
