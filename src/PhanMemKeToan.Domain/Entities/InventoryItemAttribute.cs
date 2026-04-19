using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class InventoryItemAttribute : AuditableEntity
{
    public Guid InventoryItemId { get; set; }
    public Guid AttributeTypeId { get; set; }    // FK → ItemAttributeType
    public string AttributeValue { get; set; } = string.Empty;  // max 500

    // Navigation
    public InventoryItem InventoryItem { get; set; } = null!;
    public ItemAttributeType AttributeType { get; set; } = null!;
}
