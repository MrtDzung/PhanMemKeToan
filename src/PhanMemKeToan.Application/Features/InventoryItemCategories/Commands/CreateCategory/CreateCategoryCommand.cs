using MediatR;

namespace PhanMemKeToan.Application.Features.InventoryItemCategories.Commands.CreateCategory;

public record CreateCategoryCommand(
    string CategoryCode,
    string CategoryName,
    Guid? ParentId,
    bool IsActive,
    int SortOrder
) : IRequest<Guid>;
