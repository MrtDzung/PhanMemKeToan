using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Features.InventoryItemCategories.Commands.CreateCategory;
using PhanMemKeToan.Application.Features.InventoryItemCategories.Commands.DeleteCategory;
using PhanMemKeToan.Application.Features.InventoryItemCategories.Commands.UpdateCategory;
using PhanMemKeToan.Application.Features.InventoryItemCategories.Queries.GetCategoryTree;
using PhanMemKeToan.Application.Features.InventoryItems.Commands.CreateInventoryItem;
using PhanMemKeToan.Application.Features.InventoryItems.Commands.DeleteInventoryItem;
using PhanMemKeToan.Application.Features.InventoryItems.Commands.UpdateInventoryItem;
using PhanMemKeToan.Application.Features.InventoryItems.Queries.GetInventoryItemById;
using PhanMemKeToan.Application.Features.InventoryItems.Queries.GetInventoryItems;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Authorize]
public class InventoryItemsController(ISender sender) : ControllerBase
{
    // ─── Inventory Items ─────────────────────────────────────────────────────

    [HttpGet("api/inventory-items")]
    [Authorize(Policy = "IN.InventoryItems.View")]
    public async Task<IActionResult> GetInventoryItems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] int? itemType = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetInventoryItemsQuery(page, pageSize, categoryId, itemType, search, isActive),
            cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpGet("api/inventory-items/{id:guid}")]
    [Authorize(Policy = "IN.InventoryItems.View")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInventoryItemByIdQuery(id), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpPost("api/inventory-items")]
    [Authorize(Policy = "IN.InventoryItems.Manage")]
    public async Task<IActionResult> CreateInventoryItem(
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateInventoryItemCommand(
            request.ItemCode, request.ItemName, request.ItemNameEnglish, request.Description,
            request.ItemType, request.CostingMethod, request.UnitId, request.CategoryId,
            request.DefaultTaxRate, request.UnitPrice, request.SalePrice1, request.SalePrice2,
            request.SalePrice3, request.MinStockLevel, request.MaxStockLevel, request.LeadTimeDays,
            request.IsFollowSerial, request.IsFollowLot, request.IsFollowExpiry,
            request.IsPanelItem, request.PanelUnitId, request.FormulaTemplateId, request.IsActive,
            request.UnitConverts, request.Barcodes, request.ItemAttributes, request.OpeningBalances);

        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id },
            new { data = new { id }, errors = Array.Empty<object>() });
    }

    [HttpPut("api/inventory-items/{id:guid}")]
    [Authorize(Policy = "IN.InventoryItems.Manage")]
    public async Task<IActionResult> UpdateInventoryItem(
        Guid id,
        [FromBody] UpdateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateInventoryItemCommand(
            id, request.RowVersion,
            request.ItemCode, request.ItemName, request.ItemNameEnglish, request.Description,
            request.ItemType, request.CostingMethod, request.UnitId, request.CategoryId,
            request.DefaultTaxRate, request.UnitPrice, request.SalePrice1, request.SalePrice2,
            request.SalePrice3, request.MinStockLevel, request.MaxStockLevel, request.LeadTimeDays,
            request.IsFollowSerial, request.IsFollowLot, request.IsFollowExpiry,
            request.IsPanelItem, request.PanelUnitId, request.FormulaTemplateId, request.IsActive,
            request.UnitConverts, request.Barcodes, request.ItemAttributes, request.OpeningBalances);

        var newRowVersion = await sender.Send(command, cancellationToken);
        return Ok(new { data = new { id, rowVersion = newRowVersion }, errors = Array.Empty<object>() });
    }

    [HttpDelete("api/inventory-items/{id:guid}")]
    [Authorize(Policy = "IN.InventoryItems.Manage")]
    public async Task<IActionResult> DeleteInventoryItem(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteInventoryItemCommand(id), cancellationToken);
        return NoContent();
    }

    // ─── Inventory Item Categories ────────────────────────────────────────────

    [HttpGet("api/inventory-item-categories")]
    [Authorize(Policy = "IN.InventoryItems.View")]
    public async Task<IActionResult> GetCategoryTree(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCategoryTreeQuery(), cancellationToken);
        return Ok(new { data = result, errors = Array.Empty<object>() });
    }

    [HttpPost("api/inventory-item-categories")]
    [Authorize(Policy = "IN.InventoryItems.Manage")]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new CreateCategoryCommand(request.CategoryCode, request.CategoryName, request.ParentId, request.IsActive, request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetCategoryTree), new { },
            new { data = new { id }, errors = Array.Empty<object>() });
    }

    [HttpPut("api/inventory-item-categories/{id:guid}")]
    [Authorize(Policy = "IN.InventoryItems.Manage")]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateCategoryCommand(id, request.CategoryCode, request.CategoryName, request.ParentId, request.IsActive, request.SortOrder),
            cancellationToken);
        return Ok(new { data = new { id }, errors = Array.Empty<object>() });
    }

    [HttpDelete("api/inventory-item-categories/{id:guid}")]
    [Authorize(Policy = "IN.InventoryItems.Manage")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return NoContent();
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public record CreateInventoryItemRequest(
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
);

public record UpdateInventoryItemRequest(
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
);

public record CreateCategoryRequest(
    string CategoryCode,
    string CategoryName,
    Guid? ParentId,
    bool IsActive,
    int SortOrder
);

public record UpdateCategoryRequest(
    string CategoryCode,
    string CategoryName,
    Guid? ParentId,
    bool IsActive,
    int SortOrder
);
