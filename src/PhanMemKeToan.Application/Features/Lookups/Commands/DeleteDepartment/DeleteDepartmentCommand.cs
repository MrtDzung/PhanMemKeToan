using MediatR;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.DeleteDepartment;

public record DeleteDepartmentCommand(Guid Id) : IRequest;
