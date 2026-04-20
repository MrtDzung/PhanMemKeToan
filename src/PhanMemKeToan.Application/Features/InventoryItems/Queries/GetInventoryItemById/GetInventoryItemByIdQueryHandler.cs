using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.InventoryItems.DTOs;

namespace PhanMemKeToan.Application.Features.InventoryItems.Queries.GetInventoryItemById;

public class GetInventoryItemByIdQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<GetInventoryItemByIdQuery, InventoryItemDetailDto>
{
    public async Task<InventoryItemDetailDto> Handle(
        GetInventoryItemByIdQuery request, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var entity = await dbContext.InventoryItems
            .AsNoTracking()
            .Include(i => i.Unit)
            .Include(i => i.Category)
            .Include(i => i.UnitConverts)
                .ThenInclude(uc => uc.Unit)
            .Include(i => i.Barcodes)
            .Include(i => i.ItemAttributes)
                .ThenInclude(a => a.AttributeType)
            .Include(i => i.OpeningBalances)
                .ThenInclude(ob => ob.Warehouse)
            .Include(i => i.OpeningBalances)
                .ThenInclude(ob => ob.Unit)
            .Include(i => i.OpeningBalances)
                .ThenInclude(ob => ob.Currency)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.InventoryItem), request.Id);

        return new InventoryItemDetailDto
        {
            Id = entity.Id,
            ItemCode = entity.ItemCode,
            ItemName = entity.ItemName,
            ItemNameEnglish = entity.ItemNameEnglish,
            Description = entity.Description,
            ItemType = (int)entity.ItemType,
            CostingMethod = (int)entity.CostingMethod,
            UnitId = entity.UnitId,
            UnitCode = entity.Unit.UnitCode,
            CategoryId = entity.CategoryId,
            CategoryName = entity.Category?.CategoryName,
            DefaultTaxRate = entity.DefaultTaxRate,
            UnitPrice = entity.UnitPrice,
            SalePrice1 = entity.SalePrice1,
            SalePrice2 = entity.SalePrice2,
            SalePrice3 = entity.SalePrice3,
            MinStockLevel = entity.MinStockLevel,
            MaxStockLevel = entity.MaxStockLevel,
            LeadTimeDays = entity.LeadTimeDays,
            IsFollowSerial = entity.IsFollowSerial,
            IsFollowLot = entity.IsFollowLot,
            IsFollowExpiry = entity.IsFollowExpiry,
            IsPanelItem = entity.IsPanelItem,
            PanelUnitId = entity.PanelUnitId,
            FormulaTemplateId = entity.FormulaTemplateId,
            IsActive = entity.IsActive,
            RowVersion = entity.RowVersion,
            UnitConverts = entity.UnitConverts.Select(uc => new UnitConvertDto
            {
                Id = uc.Id,
                UnitId = uc.UnitId,
                UnitCode = uc.Unit.UnitCode,
                UnitName = uc.Unit.UnitName,
                ConvertRate = uc.ConvertRate,
            }).ToList(),
            Barcodes = entity.Barcodes.Select(b => new BarcodeDto
            {
                Id = b.Id,
                BarcodeValue = b.BarcodeValue,
                BarcodeType = (int)b.BarcodeType,
                IsPrimary = false,
            }).ToList(),
            ItemAttributes = entity.ItemAttributes.Select(a => new ItemAttributeDto
            {
                Id = a.Id,
                AttributeTypeId = a.AttributeTypeId,
                AttributeCode = a.AttributeType.AttributeCode,
                AttributeName = a.AttributeType.AttributeName,
                AttributeValue = a.AttributeValue,
            }).ToList(),
            OpeningBalances = entity.OpeningBalances.Select(ob => new InventoryItemOpeningBalanceDto
            {
                Id = ob.Id,
                WarehouseId = ob.WarehouseId,
                WarehouseName = ob.Warehouse.WarehouseName,
                UnitId = ob.UnitId,
                UnitCode = ob.Unit.UnitCode,
                Quantity = ob.Quantity,
                UnitCost = ob.UnitCost,
                Amount = ob.Amount,
                CurrencyId = ob.CurrencyId,
                CurrencyCode = ob.Currency?.CurrencyCode,
                ForeignAmount = ob.ForeignAmount,
                ExchangeRate = ob.ExchangeRate,
            }).ToList(),
        };
    }
}
