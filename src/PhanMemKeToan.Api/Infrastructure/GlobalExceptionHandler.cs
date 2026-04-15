using Microsoft.AspNetCore.Diagnostics;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Domain.Common.Exceptions;

namespace PhanMemKeToan.Api.Infrastructure;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, errorCode, extras) = exception switch
        {
            InvalidCredentialsException => (401, "INVALID_CREDENTIALS", (object?)null),
            AccountDeactivatedException => (401, "ACCOUNT_DEACTIVATED", (object?)null),
            AccountLockedException ale => (429, "ACCOUNT_LOCKED", (object?)new { lockedUntil = ale.LockedUntil }),
            TenantNotFoundException => (403, "TENANT_NOT_FOUND", (object?)null),
            TenantDeactivatedException => (403, "TENANT_DEACTIVATED", (object?)null),
            TokenExpiredException => (401, "TOKEN_EXPIRED", (object?)null),
            TokenRevokedException => (401, "TOKEN_REVOKED", (object?)null),
            DuplicateEmailException dee => (409, "DUPLICATE_EMAIL", (object?)new { email = dee.Email }),
            RoleInUseException riue => (409, "ROLE_IN_USE", (object?)new { affectedUsers = riue.AffectedUserNames }),
            NotFoundException nfe => (404, "NOT_FOUND", (object?)new { entity = nfe.EntityName }),
            ValidationException ve => (400, "VALIDATION_ERROR", (object?)new { errors = ve.Errors }),
            ForbiddenAccessException => (403, "FORBIDDEN", (object?)null),
            _ => (500, "INTERNAL_ERROR", (object?)null)
        };

        var response = new
        {
            errorCode,
            message = exception.Message,
            extras
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}

