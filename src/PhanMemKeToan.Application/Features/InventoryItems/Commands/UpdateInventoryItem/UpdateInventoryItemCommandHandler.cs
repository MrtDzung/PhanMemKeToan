using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.InventoryItems.Commands.UpdateInventoryItem;

public class UpdateInventoryItemCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<UpdateInventoryItemCommand, int>
{
    public async Task<int> Handle(UpdateInventoryItemCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var entity = await dbContext.InventoryItems
            .Include(i => i.UnitConverts)
            .Include(i => i.Barcodes)
            .Include(i => i.ItemAttributes)
            .Include(i => i.OpeningBalances)
            .FirstOrDefaultAsync(i => i.Id == cmd.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryItem), cmd.Id);

        // Optimistic concurrency check
        if (entity.RowVersion != cmd.RowVersion)
            throw new ConflictException("Record has been modified by another user. Please refresh and try again.");

        // Unique ItemCode check (only if changed)
        if (!string.Equals(entity.ItemCode, cmd.ItemCode, StringComparison.OrdinalIgnoreCase))
        {
            if (await dbContext.InventoryItems.AnyAsync(i => i.ItemCode == cmd.ItemCode, cancellationToken))
                throw new ConflictException($"ItemCode '{cmd.ItemCode}' already exists.");
        }

        // UnitId must exist
        if (!await dbContext.Units.AnyAsync(u => u.Id == cmd.UnitId, cancellationToken))
            throw new NotFoundException(nameof(Domain.Entities.Unit), cmd.UnitId);

        // CategoryId must exist if provided
        if (cmd.CategoryId.HasValue && !await dbContext.InventoryItemCategories.AnyAsync(c => c.Id == cmd.CategoryId.Value, cancellationToken))
            throw new NotFoundException(nameof(InventoryItemCategory), cmd.CategoryId.Value);

        // Service items cannot have opening balances (BR-IN05)
        if (cmd.ItemType == (int)InventoryItemType.Service && cmd.OpeningBalances.Count > 0)
            throw new BusinessRuleException("service_no_opening_balance", "Service items cannot have opening balances.");

        // Full-replace UnitConverts
        dbContext.InventoryItemUnitConverts.RemoveRange(entity.UnitConverts);
        entity.UnitConverts.Clear();

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

        // Full-replace Barcodes (validate uniqueness against other items)
        var newBarcodeValues = cmd.Barcodes.Select(b => b.BarcodeValue).ToHashSet();
        var existingBarcodeConflict = await dbContext.InventoryItemBarcodes
            .Where(b => b.InventoryItemId != cmd.Id && newBarcodeValues.Contains(b.BarcodeValue))
            .Select(b => b.BarcodeValue)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingBarcodeConflict is not null)
            throw new ConflictException($"Barcode '{existingBarcodeConflict}' is already used by another item.");

        dbContext.InventoryItemBarcodes.RemoveRange(entity.Barcodes);
        entity.Barcodes.Clear();

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

        // Full-replace ItemAttributes
        dbContext.InventoryItemAttributes.RemoveRange(entity.ItemAttributes);
        entity.ItemAttributes.Clear();

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

        // Full-replace OpeningBalances
        dbContext.InventoryItemOpeningBalances.RemoveRange(entity.OpeningBalances);
        entity.OpeningBalances.Clear();

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

        // Update main entity
        entity.ItemCode = cmd.ItemCode;
        entity.ItemName = cmd.ItemName;
        entity.ItemNameEnglish = cmd.ItemNameEnglish;
        entity.Description = cmd.Description;
        entity.ItemType = (InventoryItemType)cmd.ItemType;
        entity.CostingMethod = (CostingMethod)cmd.CostingMethod;
        entity.UnitId = cmd.UnitId;
        entity.CategoryId = cmd.CategoryId;
        entity.DefaultTaxRate = cmd.DefaultTaxRate;
        entity.UnitPrice = cmd.UnitPrice;
        entity.SalePrice1 = cmd.SalePrice1;
        entity.SalePrice2 = cmd.SalePrice2;
        entity.SalePrice3 = cmd.SalePrice3;
        entity.MinStockLevel = cmd.MinStockLevel ?? 0;
        entity.MaxStockLevel = cmd.MaxStockLevel ?? 0;
        entity.LeadTimeDays = cmd.LeadTimeDays ?? 0;
        entity.IsFollowSerial = cmd.IsFollowSerial;
        entity.IsFollowLot = cmd.IsFollowLot;
        entity.IsFollowExpiry = cmd.IsFollowExpiry;
        entity.IsPanelItem = cmd.IsPanelItem;
        entity.PanelUnitId = cmd.PanelUnitId;
        entity.FormulaTemplateId = cmd.FormulaTemplateId;
        entity.IsActive = cmd.IsActive;
        entity.RowVersion = cmd.RowVersion + 1;

        await dbContext.SaveChangesAsync(cancellationToken);

        return entity.RowVersion;
    }
}
