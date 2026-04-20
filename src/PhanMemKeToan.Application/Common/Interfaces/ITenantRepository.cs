namespace PhanMemKeToan.Application.Common.Interfaces;

public record TenantDto(Guid Id, string Code, string Name, bool IsActive);

public interface ITenantRepository
{
    Task<TenantDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
