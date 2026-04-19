namespace PhanMemKeToan.Application.Features.Lookups.DTOs;

public record UnitDto(
    Guid Id,
    string UnitCode,
    string UnitName,
    bool IsActive);
