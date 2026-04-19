using MediatR;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertWarehouse;

public record UpsertWarehouseCommand(
    Guid? Id,
    string WarehouseCode,
    string WarehouseName,
    string? Address,
    bool IsActive) : IRequest<WarehouseDto>;
