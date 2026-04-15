using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Common.Models;

namespace PhanMemKeToan.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetUsersQuery, PaginatedResult<UserListDto>>
{
    public async Task<PaginatedResult<UserListDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(search) || u.FullName.ToLower().Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.FullName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserListDto(
                u.Id, u.Email, u.FullName, u.IsActive,
                u.LockedUntil != null && u.LockedUntil > DateTimeOffset.UtcNow,
                u.LastLoginAt,
                u.UserRoles.Where(ur => !ur.Role.IsDeleted).Select(ur => ur.Role.Name).ToList()))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<UserListDto>(items, total, request.Page, request.PageSize);
    }
}
