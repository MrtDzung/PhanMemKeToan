using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class ItemAttributeType : AuditableEntity
{
    public string AttributeCode { get; set; } = string.Empty;  // max 50
    public string AttributeName { get; set; } = string.Empty;  // max 255
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<InventoryItemAttribute> ItemAttributes { get; set; } = [];
}
