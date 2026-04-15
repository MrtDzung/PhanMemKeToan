namespace PhanMemKeToan.Domain.Common.Exceptions;

public class DuplicateEmailException(string email)
    : Exception($"A user with email '{email}' already exists.")
{
    public string Email { get; } = email;
}
