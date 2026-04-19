using MediatR;
using PhanMemKeToan.Application.Common.Models;
using PhanMemKeToan.Application.Features.InventoryItems.DTOs;

namespace PhanMemKeToan.Application.Features.InventoryItems.Queries.GetInventoryItems;

public record GetInventoryItemsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    int? ItemType = null,
    string? Search = null,
    bool? IsActive = null
) : IRequest<PaginatedResult<InventoryItemListItemDto>>;
