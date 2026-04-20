namespace PhanMemKeToan.Application.Features.InventoryItems.DTOs;

public class InventoryItemDetailDto
{
    public Guid Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? ItemNameEnglish { get; set; }
    public string? Description { get; set; }
    public int ItemType { get; set; }
    public int CostingMethod { get; set; }
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal? DefaultTaxRate { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? SalePrice1 { get; set; }
    public decimal? SalePrice2 { get; set; }
    public decimal? SalePrice3 { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal MaxStockLevel { get; set; }
    public int LeadTimeDays { get; set; }
    public bool IsFollowSerial { get; set; }
    public bool IsFollowLot { get; set; }
    public bool IsFollowExpiry { get; set; }
    public bool IsPanelItem { get; set; }
    public Guid? PanelUnitId { get; set; }
    public Guid? FormulaTemplateId { get; set; }
    public bool IsActive { get; set; }
    public int RowVersion { get; set; }
    public List<UnitConvertDto> UnitConverts { get; set; } = [];
    public List<BarcodeDto> Barcodes { get; set; } = [];
    public List<ItemAttributeDto> ItemAttributes { get; set; } = [];
    public List<InventoryItemOpeningBalanceDto> OpeningBalances { get; set; } = [];
}
