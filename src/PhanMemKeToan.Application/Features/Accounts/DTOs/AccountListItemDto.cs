using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Accounts.DTOs;

public class AccountListItemDto
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int Grade { get; set; }
    public AccountCategoryKind AccountCategoryKind { get; set; }
    public bool Inactive { get; set; }
    public bool IsParent { get; set; }
    public bool IsPostableInForeignCurrency { get; set; }
}
