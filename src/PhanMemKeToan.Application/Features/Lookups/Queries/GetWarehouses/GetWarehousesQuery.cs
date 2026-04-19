using MediatR;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Queries.GetWarehouses;

public record GetWarehousesQuery(string? Search = null) : IRequest<List<WarehouseDto>>;
