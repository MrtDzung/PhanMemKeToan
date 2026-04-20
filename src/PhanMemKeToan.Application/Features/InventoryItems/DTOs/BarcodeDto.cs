namespace PhanMemKeToan.Application.Features.InventoryItems.DTOs;

public class BarcodeDto
{
    public Guid Id { get; set; }
    public string BarcodeValue { get; set; } = string.Empty;
    public int BarcodeType { get; set; }
    public Guid? UnitId { get; set; }
    public bool IsPrimary { get; set; }
}
