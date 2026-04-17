using MediatR;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.ToggleTenantStatus;

public record ToggleTenantStatusCommand(
    Guid Id,
    bool Activate
) : IRequest;
