using MediatR;
using PhanMemKeToan.Application.Features.InventoryItems.DTOs;

namespace PhanMemKeToan.Application.Features.InventoryItems.Queries.GetInventoryItemById;

public record GetInventoryItemByIdQuery(Guid Id) : IRequest<InventoryItemDetailDto>;
