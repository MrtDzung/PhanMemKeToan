using MediatR;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertDepartment;

public record UpsertDepartmentCommand(
    Guid? Id,
    string DepartmentCode,
    string DepartmentName,
    Guid? ParentId,
    bool IsActive) : IRequest<DepartmentDto>;
