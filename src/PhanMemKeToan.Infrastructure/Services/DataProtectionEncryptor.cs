using Microsoft.AspNetCore.DataProtection;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Infrastructure.Services;

public class DataProtectionEncryptor(IDataProtectionProvider dataProtectionProvider)
    : IConnectionStringEncryptor
{
    private const string Purpose = "TenantConnectionString";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(Purpose);

    public string Encrypt(string plainConnectionString)
    {
        return _protector.Protect(plainConnectionString);
    }

    public string Decrypt(string encryptedConnectionString)
    {
        return _protector.Unprotect(encryptedConnectionString);
    }
}
