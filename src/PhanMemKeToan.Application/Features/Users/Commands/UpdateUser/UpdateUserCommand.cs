using MediatR;

namespace PhanMemKeToan.Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid UserId,
    string FullName,
    string? Email,
    IReadOnlyList<Guid> RoleIds
) : IRequest;
