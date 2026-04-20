using MediatR;
using PhanMemKeToan.Application.Features.AccountObjects.DTOs;

namespace PhanMemKeToan.Application.Features.AccountObjects.Queries.GetAccountObjectById;

public record GetAccountObjectByIdQuery(Guid Id) : IRequest<AccountObjectDetailDto>;
