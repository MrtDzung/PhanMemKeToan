using MediatR;

namespace PhanMemKeToan.Application.Features.Users.Commands.ToggleUserActivation;

public record ToggleUserActivationCommand(Guid UserId, bool Activate) : IRequest;
