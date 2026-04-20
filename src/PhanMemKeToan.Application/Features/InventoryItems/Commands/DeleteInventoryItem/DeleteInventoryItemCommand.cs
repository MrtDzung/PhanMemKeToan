using MediatR;

namespace PhanMemKeToan.Application.Features.InventoryItems.Commands.DeleteInventoryItem;

public record DeleteInventoryItemCommand(Guid Id) : IRequest;
