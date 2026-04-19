namespace PhanMemKeToan.Application.Features.Lookups.DTOs;

public record WarehouseDto(
    Guid Id,
    string WarehouseCode,
    string WarehouseName,
    string? Address,
    bool IsActive);
