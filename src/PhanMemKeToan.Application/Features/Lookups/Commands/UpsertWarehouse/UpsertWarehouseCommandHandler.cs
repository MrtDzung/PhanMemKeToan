using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Lookups.DTOs;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertWarehouse;

public class UpsertWarehouseCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpsertWarehouseCommand, WarehouseDto>
{
    public async Task<WarehouseDto> Handle(UpsertWarehouseCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var codeExists = await dbContext.Warehouses.AnyAsync(
            w => w.TenantId == tenantId && w.WarehouseCode == cmd.WarehouseCode && w.Id != cmd.Id,
            cancellationToken);

        if (codeExists)
            throw new ConflictException($"WarehouseCode '{cmd.WarehouseCode}' đã tồn tại.");

        Warehouse entity;

        if (cmd.Id == null)
        {
            entity = new Warehouse
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                WarehouseCode = cmd.WarehouseCode,
                WarehouseName = cmd.WarehouseName,
                Address = cmd.Address,
                IsActive = cmd.IsActive,
            };
            dbContext.Warehouses.Add(entity);
        }
        else
        {
            entity = await dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.Id == cmd.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Warehouse), cmd.Id);

            entity.WarehouseCode = cmd.WarehouseCode;
            entity.WarehouseName = cmd.WarehouseName;
            entity.Address = cmd.Address;
            entity.IsActive = cmd.IsActive;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new WarehouseDto(entity.Id, entity.WarehouseCode, entity.WarehouseName, entity.Address, entity.IsActive);
    }
}
