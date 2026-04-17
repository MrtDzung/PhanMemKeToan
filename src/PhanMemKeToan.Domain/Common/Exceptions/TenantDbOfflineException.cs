namespace PhanMemKeToan.Domain.Common.Exceptions;

public class TenantDbOfflineException(string tenantName)
    : Exception($"Database for tenant '{tenantName}' is currently offline.")
{
    public string TenantName { get; } = tenantName;
}
