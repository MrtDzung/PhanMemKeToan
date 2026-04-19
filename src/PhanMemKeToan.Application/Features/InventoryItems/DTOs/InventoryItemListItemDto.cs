namespace PhanMemKeToan.Application.Features.InventoryItems.DTOs;

public class InventoryItemListItemDto
{
    public Guid Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int ItemType { get; set; }
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsActive { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? SalePrice1 { get; set; }
}
