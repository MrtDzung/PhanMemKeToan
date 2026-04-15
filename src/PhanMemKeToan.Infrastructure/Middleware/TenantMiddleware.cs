using Microsoft.AspNetCore.Http;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Infrastructure.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    private const string TenantCodeHeader = "X-Tenant-Code";

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantRepository tenantRepository)
    {
        // Skip tenant resolution for public endpoints
        if (context.Request.Path.StartsWithSegments("/.well-known") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        TenantDto? tenant = null;

        // Priority 1: JWT tid claim
        var tidClaim = context.User.FindFirst("tid")?.Value;
        if (!string.IsNullOrEmpty(tidClaim) && Guid.TryParse(tidClaim, out var tenantIdFromJwt))
        {
            tenant = await tenantRepository.GetByIdAsync(tenantIdFromJwt, context.RequestAborted);
        }

        // Priority 2: X-Tenant-Code header (for unauthenticated requests)
        if (tenant is null && context.Request.Headers.TryGetValue(TenantCodeHeader, out var tenantCode))
        {
            tenant = await tenantRepository.GetByCodeAsync(tenantCode!, context.RequestAborted);
        }

        // Resolve tenant
        if (tenant is null)
        {
            // Allow unauthenticated requests to reach auth endpoints (login doesn't require tenant yet)
            if (context.Request.Path.StartsWithSegments("/api/auth/login"))
            {
                await next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                title = "Forbidden",
                status = 403,
                errorCode = "TENANT_NOT_FOUND"
            });
            return;
        }

        if (!tenant.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                title = "Forbidden",
                status = 403,
                errorCode = "TENANT_DEACTIVATED"
            });
            return;
        }

        tenantContext.TenantId = tenant.Id;
        await next(context);
    }
}
