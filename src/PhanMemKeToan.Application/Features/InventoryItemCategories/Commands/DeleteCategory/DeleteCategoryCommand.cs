using MediatR;

namespace PhanMemKeToan.Application.Features.InventoryItemCategories.Commands.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : IRequest;
