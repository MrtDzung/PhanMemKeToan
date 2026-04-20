using MediatR;

namespace PhanMemKeToan.Application.Features.AccountObjects.Commands.DeleteAccountObject;

public record DeleteAccountObjectCommand(Guid Id) : IRequest;
