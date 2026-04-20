using FluentValidation;
using PhanMemKeToan.Application.Features.InventoryItems.Commands.UpdateInventoryItem;

namespace PhanMemKeToan.Application.Features.InventoryItems.Validators;

public class UpdateInventoryItemCommandValidator : AbstractValidator<UpdateInventoryItemCommand>
{
    public UpdateInventoryItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ItemCode).NotEmpty().MaximumLength(25);
        RuleFor(x => x.ItemName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.ItemType).InclusiveBetween(0, 3)
            .WithMessage("ItemType must be 0 (Goods), 1 (RawMaterial), 2 (FinishedProduct), or 3 (Service).");
        RuleFor(x => x.CostingMethod).InclusiveBetween(1, 4)
            .When(x => x.CostingMethod != 0)
            .WithMessage("CostingMethod must be 1 (FIFO), 2 (LIFO), 3 (WeightedAverage), or 4 (SpecificIdentification).");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).When(x => x.UnitPrice.HasValue);
        RuleFor(x => x.SalePrice1).GreaterThanOrEqualTo(0).When(x => x.SalePrice1.HasValue);
        RuleFor(x => x.SalePrice2).GreaterThanOrEqualTo(0).When(x => x.SalePrice2.HasValue);
        RuleFor(x => x.SalePrice3).GreaterThanOrEqualTo(0).When(x => x.SalePrice3.HasValue);
        RuleFor(x => x.MinStockLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxStockLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LeadTimeDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ItemNameEnglish).MaximumLength(255).When(x => x.ItemNameEnglish != null);

        RuleForEach(x => x.UnitConverts).ChildRules(uc =>
        {
            uc.RuleFor(u => u.UnitId).NotEmpty();
            uc.RuleFor(u => u.ConvertRate).GreaterThan(0);
        });

        RuleForEach(x => x.Barcodes).ChildRules(b =>
        {
            b.RuleFor(bc => bc.BarcodeValue).NotEmpty().MaximumLength(255);
            b.RuleFor(bc => bc.BarcodeType).InclusiveBetween(0, 5);
        });

        RuleForEach(x => x.OpeningBalances).ChildRules(ob =>
        {
            ob.RuleFor(o => o.WarehouseId).NotEmpty();
            ob.RuleFor(o => o.UnitId).NotEmpty();
            ob.RuleFor(o => o.Quantity).GreaterThanOrEqualTo(0);
            ob.RuleFor(o => o.UnitCost).GreaterThanOrEqualTo(0);
            ob.RuleFor(o => o.Amount).GreaterThanOrEqualTo(0);
            ob.RuleFor(o => o.ExchangeRate).GreaterThan(0);
        });
    }
}
