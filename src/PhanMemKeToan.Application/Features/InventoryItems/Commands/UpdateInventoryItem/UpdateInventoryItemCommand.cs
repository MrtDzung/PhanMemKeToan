using MediatR;

namespace PhanMemKeToan.Application.Features.InventoryItems.Commands.UpdateInventoryItem;

public record UpdateInventoryItemCommand(
    Guid Id,
    int RowVersion,
    string ItemCode,
    string ItemName,
    string? ItemNameEnglish,
    string? Description,
    int ItemType,
    int CostingMethod,
    Guid UnitId,
    Guid? CategoryId,
    decimal? DefaultTaxRate,
    decimal? UnitPrice,
    decimal? SalePrice1,
    decimal? SalePrice2,
    decimal? SalePrice3,
    decimal? MinStockLevel,
    decimal? MaxStockLevel,
    int? LeadTimeDays,
    bool IsFollowSerial,
    bool IsFollowLot,
    bool IsFollowExpiry,
    bool IsPanelItem,
    Guid? PanelUnitId,
    Guid? FormulaTemplateId,
    bool IsActive,
    List<UpdateUnitConvertDto> UnitConverts,
    List<UpdateBarcodeDto> Barcodes,
    List<UpdateItemAttributeDto> ItemAttributes,
    List<UpdateOpeningBalanceDto> OpeningBalances
) : IRequest<int>;

public record UpdateUnitConvertDto(
    Guid? Id,
    Guid UnitId,
    decimal ConvertRate
);

public record UpdateBarcodeDto(
    Guid? Id,
    string BarcodeValue,
    int BarcodeType
);

public record UpdateItemAttributeDto(
    Guid? Id,
    Guid AttributeTypeId,
    string AttributeValue
);

public record UpdateOpeningBalanceDto(
    Guid? Id,
    Guid WarehouseId,
    Guid UnitId,
    decimal Quantity,
    decimal UnitCost,
    decimal Amount,
    Guid? CurrencyId,
    decimal? ForeignAmount,
    decimal ExchangeRate
);
