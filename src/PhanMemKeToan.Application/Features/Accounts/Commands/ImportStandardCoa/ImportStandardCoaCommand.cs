using MediatR;
using PhanMemKeToan.Application.Features.Accounts.DTOs;

namespace PhanMemKeToan.Application.Features.Accounts.Commands.ImportStandardCoa;

public record ImportStandardCoaCommand(
    string Standard,
    string ConflictResolution,
    bool DryRun = false
) : IRequest<ImportCoaResultDto>;
