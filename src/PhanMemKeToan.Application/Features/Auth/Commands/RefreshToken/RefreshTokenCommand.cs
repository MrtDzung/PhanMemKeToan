using MediatR;

namespace PhanMemKeToan.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshTokenPlaintext) : IRequest<RefreshTokenResult>;

public record RefreshTokenResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
