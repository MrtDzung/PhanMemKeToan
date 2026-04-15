using MediatR;

namespace PhanMemKeToan.Application.Features.Users.Commands.UpdateProfile;

public record UpdateProfileCommand(Guid UserId, string FullName) : IRequest;
