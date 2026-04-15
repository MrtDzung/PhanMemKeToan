namespace PhanMemKeToan.Domain.Common.Exceptions;

public class TenantNotFoundException(string identifier)
    : Exception($"Tenant '{identifier}' was not found.")
{
}
