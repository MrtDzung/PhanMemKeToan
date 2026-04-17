namespace PhanMemKeToan.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid TokenFamily { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public bool IsRevoked { get; set; }

    public User User { get; set; } = null!;
}
