using MediatR;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.DeleteCurrency;

public record DeleteCurrencyCommand(Guid Id) : IRequest;
