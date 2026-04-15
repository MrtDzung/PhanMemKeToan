namespace PhanMemKeToan.Domain.Common.Exceptions;

public class AccountLockedException(DateTimeOffset lockedUntil)
    : Exception($"This account is locked until {lockedUntil:O}.")
{
    public DateTimeOffset LockedUntil { get; } = lockedUntil;
}
