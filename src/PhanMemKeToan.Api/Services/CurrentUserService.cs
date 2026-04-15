using System.Security.Claims;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    public Guid? TenantId
    {
        get
        {
            var tenantClaim = httpContextAccessor.HttpContext?.User?.FindFirstValue("tenant_id");
            return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : null;
        }
    }
}
