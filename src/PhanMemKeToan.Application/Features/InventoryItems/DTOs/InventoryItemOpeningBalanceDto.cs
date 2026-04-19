namespace PhanMemKeToan.Application.Features.InventoryItems.DTOs;

public class InventoryItemOpeningBalanceDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Amount { get; set; }
    public Guid? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? ForeignAmount { get; set; }
    public decimal? ExchangeRate { get; set; }
}
