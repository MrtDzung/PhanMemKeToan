namespace PhanMemKeToan.Application.Features.AccountObjects.DTOs;

public class AccountObjectListItemDto
{
    public Guid Id { get; set; }
    public string ObjectCode { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public int ObjectType { get; set; }
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
