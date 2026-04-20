using MediatR;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Queries.GetExpenseItems;

public record GetExpenseItemsQuery(string? Search = null) : IRequest<List<ExpenseItemDto>>;
