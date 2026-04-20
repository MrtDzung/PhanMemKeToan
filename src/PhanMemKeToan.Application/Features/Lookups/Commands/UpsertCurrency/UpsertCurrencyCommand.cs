using MediatR;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertCurrency;

public record UpsertCurrencyCommand(
    Guid? Id,
    string CurrencyCode,
    string CurrencyName,
    string? CurrencyNameEnglish,
    string? Symbol,
    decimal ExchangeRate,
    bool IsActive) : IRequest<CurrencyDto>;
