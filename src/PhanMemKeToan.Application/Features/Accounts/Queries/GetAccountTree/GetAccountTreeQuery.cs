using MediatR;
using PhanMemKeToan.Application.Features.Accounts.DTOs;

namespace PhanMemKeToan.Application.Features.Accounts.Queries.GetAccountTree;

public record GetAccountTreeQuery(bool IncludeInactive = false, string Format = "tree") : IRequest<object>;
