using MediatR;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Queries.GetCurrencies;

public record GetCurrenciesQuery : IRequest<List<CurrencyDto>>;
