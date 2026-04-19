using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Lookups.DTOs;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertDepartment;

public class UpsertDepartmentCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpsertDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(UpsertDepartmentCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var codeExists = await dbContext.Departments.AnyAsync(
            d => d.TenantId == tenantId && d.DepartmentCode == cmd.DepartmentCode && d.Id != cmd.Id,
            cancellationToken);

        if (codeExists)
            throw new ConflictException($"DepartmentCode '{cmd.DepartmentCode}' đã tồn tại.");

        int level = 1;
        if (cmd.ParentId.HasValue)
        {
            var parent = await dbContext.Departments
                .FirstOrDefaultAsync(d => d.Id == cmd.ParentId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Department), cmd.ParentId.Value);

            level = parent.Level + 1;

            if (level > 5)
                throw new BusinessRuleException("max_level_exceeded", "Cấp bộ phận không được vượt quá 5.");
        }

        Department entity;

        if (cmd.Id == null)
        {
            entity = new Department
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                DepartmentCode = cmd.DepartmentCode,
                DepartmentName = cmd.DepartmentName,
                ParentId = cmd.ParentId,
                Level = level,
                IsActive = cmd.IsActive,
            };
            dbContext.Departments.Add(entity);
        }
        else
        {
            entity = await dbContext.Departments
                .FirstOrDefaultAsync(d => d.Id == cmd.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Department), cmd.Id);

            entity.DepartmentCode = cmd.DepartmentCode;
            entity.DepartmentName = cmd.DepartmentName;
            entity.ParentId = cmd.ParentId;
            entity.Level = level;
            entity.IsActive = cmd.IsActive;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new DepartmentDto(
            entity.Id,
            entity.DepartmentCode,
            entity.DepartmentName,
            entity.ParentId,
            entity.Level,
            entity.IsActive);
    }
}
