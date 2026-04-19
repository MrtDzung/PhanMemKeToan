using MediatR;

namespace PhanMemKeToan.Application.Features.InventoryItemCategories.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string CategoryCode,
    string CategoryName,
    Guid? ParentId,
    bool IsActive,
    int SortOrder
) : IRequest;
