using MediatR;

namespace PhanMemKeToan.Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(
    string Email,
    string Password,
    string FullName,
    IReadOnlyList<Guid> RoleIds
) : IRequest<Guid>;
