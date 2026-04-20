using MediatR;

namespace PhanMemKeToan.Application.Features.Users.Commands.UnlockUser;

public record UnlockUserCommand(Guid UserId) : IRequest;
