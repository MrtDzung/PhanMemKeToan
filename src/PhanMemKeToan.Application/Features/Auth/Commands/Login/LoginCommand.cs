using MediatR;
using PhanMemKeToan.Application.Common.Models;

namespace PhanMemKeToan.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password,
    bool RememberMe,
    string IpAddress
) : IRequest<LoginResult>;

public record LoginResult(
    string TempToken,
    IReadOnlyList<CompanyInfo> Companies,
    bool RememberMe
);
