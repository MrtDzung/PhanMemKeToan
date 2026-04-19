using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class InventoryQuantityFormulaTemplate : AuditableEntity
{
    public string TemplateCode { get; set; } = string.Empty;  // max 50
    public string TemplateName { get; set; } = string.Empty;  // max 255
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<InventoryQuantityFormulaDetail> Details { get; set; } = [];
    public ICollection<InventoryItem> Items { get; set; } = [];
}
