using Microsoft.AspNetCore.Authorization;

namespace PhanMemKeToan.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Permissions are stored as individual claims with type "permission"
        var hasPermission = context.User.Claims
            .Where(c => c.Type == "permission")
            .Any(c => c.Value == requirement.PermissionCode);

        if (hasPermission)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
