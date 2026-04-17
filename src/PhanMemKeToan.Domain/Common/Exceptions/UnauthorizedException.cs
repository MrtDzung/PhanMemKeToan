namespace PhanMemKeToan.Domain.Common.Exceptions;

public class UnauthorizedException(string errorCode, string message)
    : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
