using MediatR;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.DeleteUnit;

public record DeleteUnitCommand(Guid Id) : IRequest;
