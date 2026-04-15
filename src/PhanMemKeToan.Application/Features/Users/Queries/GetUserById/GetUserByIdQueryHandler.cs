using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetUserByIdQuery, UserDetailDto>
{
    public async Task<UserDetailDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        return new UserDetailDto(
            user.Id, user.Email, user.FullName, user.IsActive,
            user.LockedUntil.HasValue && user.LockedUntil > DateTimeOffset.UtcNow,
            user.LastLoginAt, user.CreatedAt,
            user.UserRoles.Where(ur => !ur.Role.IsDeleted).Select(ur => new RoleRefDto(ur.Role.Id, ur.Role.Name)).ToList()
        );
    }
}
