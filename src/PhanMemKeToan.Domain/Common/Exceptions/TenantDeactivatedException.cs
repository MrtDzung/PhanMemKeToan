namespace PhanMemKeToan.Domain.Common.Exceptions;

public class TenantDeactivatedException(string tenantCode)
    : Exception($"Tenant '{tenantCode}' has been deactivated.")
{
}
