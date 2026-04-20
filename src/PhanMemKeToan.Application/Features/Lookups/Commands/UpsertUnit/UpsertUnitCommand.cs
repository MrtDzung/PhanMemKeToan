using MediatR;
using PhanMemKeToan.Application.Features.Lookups.DTOs;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertUnit;

public record UpsertUnitCommand(
    Guid? Id,
    string UnitCode,
    string UnitName,
    bool IsActive) : IRequest<UnitDto>;
