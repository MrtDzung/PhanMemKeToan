namespace PhanMemKeToan.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    Guid? TenantId { get; }
}
