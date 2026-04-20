using MediatR;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Queries.GetDepartments;

public record GetDepartmentsQuery(string? Search = null) : IRequest<List<DepartmentDto>>;
