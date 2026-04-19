using MediatR;

namespace PhanMemKeToan.Application.Features.InventoryItems.Commands.CreateInventoryItem;

public record CreateInventoryItemCommand(
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
    List<CreateUnitConvertDto> UnitConverts,
    List<CreateBarcodeDto> Barcodes,
    List<CreateItemAttributeDto> ItemAttributes,
    List<CreateOpeningBalanceDto> OpeningBalances
) : IRequest<Guid>;

public record CreateUnitConvertDto(
    Guid UnitId,
    decimal ConvertRate
);

public record CreateBarcodeDto(
    string BarcodeValue,
    int BarcodeType
);

public record CreateItemAttributeDto(
    Guid AttributeTypeId,
    string AttributeValue
);

public record CreateOpeningBalanceDto(
    Guid WarehouseId,
    Guid UnitId,
    decimal Quantity,
    decimal UnitCost,
    decimal Amount,
    Guid? CurrencyId,
    decimal? ForeignAmount,
    decimal ExchangeRate
);
