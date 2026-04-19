using MediatR;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Queries.GetUnits;

public record GetUnitsQuery(string? Search = null) : IRequest<List<UnitDto>>;
