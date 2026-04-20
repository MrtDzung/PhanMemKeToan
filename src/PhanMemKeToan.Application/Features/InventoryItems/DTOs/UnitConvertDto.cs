namespace PhanMemKeToan.Application.Features.InventoryItems.DTOs;

public class UnitConvertDto
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal ConvertRate { get; set; }
}
