namespace PhanMemKeToan.Application.Features.Lookups.DTOs;

public record ExpenseItemDto(
    Guid Id,
    string ExpenseCode,
    string ExpenseName,
    bool IsActive);
