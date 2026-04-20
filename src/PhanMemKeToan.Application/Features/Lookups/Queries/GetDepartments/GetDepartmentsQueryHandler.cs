using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Queries.GetDepartments;

public class GetDepartmentsQueryHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetDepartmentsQuery, List<DepartmentDto>>
{
    public async Task<List<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var query = dbContext.Departments
            .Where(d => d.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(d => d.DepartmentCode.ToLower().Contains(search) || d.DepartmentName.ToLower().Contains(search));
        }

        return await query
            .OrderBy(d => d.Level)
            .ThenBy(d => d.DepartmentCode)
            .Select(d => new DepartmentDto(d.Id, d.DepartmentCode, d.DepartmentName, d.ParentId, d.Level, d.IsActive))
            .ToListAsync(cancellationToken);
    }
}
