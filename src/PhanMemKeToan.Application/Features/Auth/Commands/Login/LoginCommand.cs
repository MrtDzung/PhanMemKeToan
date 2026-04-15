using MediatR;

namespace PhanMemKeToan.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password,
    bool RememberMe,
    string IpAddress
) : IRequest<LoginResult>;

public record LoginResult(
    string AccessToken,
    string RefreshToken,
    bool RememberMe,
    DateTimeOffset ExpiresAt
);
