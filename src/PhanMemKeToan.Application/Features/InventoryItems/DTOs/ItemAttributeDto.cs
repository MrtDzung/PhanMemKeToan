namespace PhanMemKeToan.Application.Features.InventoryItems.DTOs;

public class ItemAttributeDto
{
    public Guid Id { get; set; }
    public Guid AttributeTypeId { get; set; }
    public string AttributeCode { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;
    public string AttributeValue { get; set; } = string.Empty;
}
