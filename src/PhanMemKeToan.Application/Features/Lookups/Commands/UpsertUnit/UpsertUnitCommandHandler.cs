using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Lookups.DTOs;
using DomainEntities = PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertUnit;

public class UpsertUnitCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpsertUnitCommand, UnitDto>
{
    public async Task<UnitDto> Handle(UpsertUnitCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var codeExists = await dbContext.Units.AnyAsync(
            u => u.TenantId == tenantId && u.UnitCode == cmd.UnitCode && u.Id != cmd.Id,
            cancellationToken);

        if (codeExists)
            throw new ConflictException($"UnitCode '{cmd.UnitCode}' đã tồn tại.");

        DomainEntities.Unit entity;

        if (cmd.Id == null)
        {
            entity = new DomainEntities.Unit
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UnitCode = cmd.UnitCode,
                UnitName = cmd.UnitName,
                IsActive = cmd.IsActive,
            };
            dbContext.Units.Add(entity);
        }
        else
        {
            entity = await dbContext.Units
                .FirstOrDefaultAsync(u => u.Id == cmd.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(DomainEntities.Unit), cmd.Id);

            entity.UnitCode = cmd.UnitCode;
            entity.UnitName = cmd.UnitName;
            entity.IsActive = cmd.IsActive;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UnitDto(entity.Id, entity.UnitCode, entity.UnitName, entity.IsActive);
    }
}
