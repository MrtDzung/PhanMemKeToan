using MediatR;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.DeleteWarehouse;

public record DeleteWarehouseCommand(Guid Id) : IRequest;
