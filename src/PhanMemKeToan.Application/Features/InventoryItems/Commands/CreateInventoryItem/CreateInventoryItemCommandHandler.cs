using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.InventoryItems.Commands.CreateInventoryItem;

public class CreateInventoryItemCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<CreateInventoryItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateInventoryItemCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        // Unique ItemCode per tenant
        if (await dbContext.InventoryItems.AnyAsync(i => i.ItemCode == cmd.ItemCode, cancellationToken))
            throw new ConflictException($"ItemCode '{cmd.ItemCode}' already exists.");

        // UnitId must exist
        if (!await dbContext.Units.AnyAsync(u => u.Id == cmd.UnitId, cancellationToken))
            throw new NotFoundException(nameof(Domain.Entities.Unit), cmd.UnitId);

        // CategoryId must exist if provided
        if (cmd.CategoryId.HasValue && !await dbContext.InventoryItemCategories.AnyAsync(c => c.Id == cmd.CategoryId.Value, cancellationToken))
            throw new NotFoundException(nameof(InventoryItemCategory), cmd.CategoryId.Value);

        // Service items cannot have opening balances (BR-IN05)
        if (cmd.ItemType == (int)InventoryItemType.Service && cmd.OpeningBalances.Count > 0)
            throw new BusinessRuleException("service_no_opening_balance", "Service items cannot have opening balances.");

        // Barcode uniqueness per tenant
        foreach (var barcode in cmd.Barcodes)
        {
            if (await dbContext.InventoryItemBarcodes.AnyAsync(b => b.BarcodeValue == barcode.BarcodeValue, cancellationToken))
                throw new ConflictException($"Barcode '{barcode.BarcodeValue}' already exists.");
        }

        var entity = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ItemCode = cmd.ItemCode,
            ItemName = cmd.ItemName,
            ItemNameEnglish = cmd.ItemNameEnglish,
            Description = cmd.Description,
            ItemType = (InventoryItemType)cmd.ItemType,
            CostingMethod = (CostingMethod)cmd.CostingMethod,
            UnitId = cmd.UnitId,
            CategoryId = cmd.CategoryId,
            DefaultTaxRate = cmd.DefaultTaxRate,
            UnitPrice = cmd.UnitPrice,
            SalePrice1 = cmd.SalePrice1,
            SalePrice2 = cmd.SalePrice2,
            SalePrice3 = cmd.SalePrice3,
            MinStockLevel = cmd.MinStockLevel ?? 0,
            MaxStockLevel = cmd.MaxStockLevel ?? 0,
            LeadTimeDays = cmd.LeadTimeDays ?? 0,
            IsFollowSerial = cmd.IsFollowSerial,
            IsFollowLot = cmd.IsFollowLot,
            IsFollowExpiry = cmd.IsFollowExpiry,
            IsPanelItem = cmd.IsPanelItem,
            PanelUnitId = cmd.PanelUnitId,
            FormulaTemplateId = cmd.FormulaTemplateId,
            IsActive = cmd.IsActive,
            RowVersion = 0,
        };

        dbContext.InventoryItems.Add(entity);

        foreach (var uc in cmd.UnitConverts)
        {
            dbContext.InventoryItemUnitConverts.Add(new InventoryItemUnitConvert
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InventoryItemId = entity.Id,
                UnitId = uc.UnitId,
                ConvertRate = uc.ConvertRate,
            });
        }

        foreach (var b in cmd.Barcodes)
        {
            dbContext.InventoryItemBarcodes.Add(new InventoryItemBarcode
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InventoryItemId = entity.Id,
                BarcodeValue = b.BarcodeValue,
                BarcodeType = (BarcodeType)b.BarcodeType,
            });
        }

        foreach (var a in cmd.ItemAttributes)
        {
            dbContext.InventoryItemAttributes.Add(new InventoryItemAttribute
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InventoryItemId = entity.Id,
                AttributeTypeId = a.AttributeTypeId,
                AttributeValue = a.AttributeValue,
            });
        }

        foreach (var ob in cmd.OpeningBalances)
        {
            dbContext.InventoryItemOpeningBalances.Add(new InventoryItemOpeningBalance
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InventoryItemId = entity.Id,
                WarehouseId = ob.WarehouseId,
                UnitId = ob.UnitId,
                Quantity = ob.Quantity,
                UnitCost = ob.UnitCost,
                Amount = ob.Amount,
                CurrencyId = ob.CurrencyId,
                ForeignAmount = ob.ForeignAmount,
                ExchangeRate = ob.ExchangeRate,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
