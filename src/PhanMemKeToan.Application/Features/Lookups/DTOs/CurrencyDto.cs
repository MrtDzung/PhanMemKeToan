namespace PhanMemKeToan.Application.Features.Lookups.DTOs;

public record CurrencyDto(
    Guid Id,
    string CurrencyCode,
    string CurrencyName,
    string? CurrencyNameEnglish,
    string? Symbol,
    decimal ExchangeRate,
    bool IsActive);
