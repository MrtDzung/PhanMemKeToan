using MediatR;

namespace PhanMemKeToan.Application.Features.Accounts.Commands.DeleteAccount;

public record DeleteAccountCommand(Guid Id, int RowVersion) : IRequest;
