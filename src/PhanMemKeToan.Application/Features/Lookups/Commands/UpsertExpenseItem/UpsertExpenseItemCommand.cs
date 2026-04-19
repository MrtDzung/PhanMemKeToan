using MediatR;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertExpenseItem;

public record UpsertExpenseItemCommand(
    Guid? Id,
    string ExpenseCode,
    string ExpenseName,
    bool IsActive) : IRequest<ExpenseItemDto>;
