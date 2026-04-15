using MediatR;
using PhanMemKeToan.Application.Features.Auth.Commands.Login;

namespace PhanMemKeToan.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshTokenPlaintext) : IRequest<LoginResult>;
