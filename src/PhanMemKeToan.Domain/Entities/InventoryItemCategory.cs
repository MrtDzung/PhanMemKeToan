using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class InventoryItemCategory : AuditableEntity
{
    public string CategoryCode { get; set; } = string.Empty;  // max 25
    public string CategoryName { get; set; } = string.Empty;  // max 255
    public Guid? ParentId { get; set; }
    public int Level { get; set; } = 1;                       // 1-5
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    // Self-referential
    public InventoryItemCategory? Parent { get; set; }
    public ICollection<InventoryItemCategory> Children { get; set; } = [];

    // Navigation
    public ICollection<InventoryItem> Items { get; set; } = [];
}
