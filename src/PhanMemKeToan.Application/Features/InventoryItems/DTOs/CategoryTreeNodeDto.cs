namespace PhanMemKeToan.Application.Features.InventoryItems.DTOs;

public class CategoryTreeNodeDto
{
    public Guid Id { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public List<CategoryTreeNodeDto> Children { get; set; } = [];
}
