using MediatR;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.DeleteExpenseItem;

public record DeleteExpenseItemCommand(Guid Id) : IRequest;
