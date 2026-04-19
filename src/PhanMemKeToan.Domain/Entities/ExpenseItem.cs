using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class ExpenseItem : AuditableEntity
{
    public string ExpenseCode { get; set; } = string.Empty;  // max 25
    public string ExpenseName { get; set; } = string.Empty;  // max 255
    public bool IsActive { get; set; } = true;
}
