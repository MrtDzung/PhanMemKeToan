namespace PhanMemKeToan.Application.Common.Interfaces;

public interface IConnectionStringEncryptor
{
    string Encrypt(string plainConnectionString);
    string Decrypt(string encryptedConnectionString);
}
