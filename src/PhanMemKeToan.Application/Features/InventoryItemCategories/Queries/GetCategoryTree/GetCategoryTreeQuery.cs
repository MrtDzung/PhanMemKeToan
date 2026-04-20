using MediatR;
using PhanMemKeToan.Application.Features.InventoryItems.DTOs;

namespace PhanMemKeToan.Application.Features.InventoryItemCategories.Queries.GetCategoryTree;

public record GetCategoryTreeQuery : IRequest<List<CategoryTreeNodeDto>>;
