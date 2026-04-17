using PhanMemKeToan.Domain.Common;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Domain.Entities;

public class Account : AuditableEntity
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? AccountNameEnglish { get; set; }
    public Guid? ParentID { get; set; }
    public int Grade { get; set; } = 1;
    public bool IsParent { get; set; }

    public AccountCategoryKind AccountCategoryKind { get; set; }
    public bool Inactive { get; set; }
    public bool IsPostableInForeignCurrency { get; set; }

    // Sub-ledger tracking flags
    public bool DetailByAccountObject { get; set; }
    public AccountObjectType AccountObjectType { get; set; }
    public bool DetailByBankAccount { get; set; }
    public bool DetailByJob { get; set; }
    public bool DetailByProjectWork { get; set; }
    public bool DetailByOrder { get; set; }
    public bool DetailByContract { get; set; }
    public bool DetailByExpenseItem { get; set; }
    public bool DetailByDepartment { get; set; }
    public bool DetailByListItem { get; set; }
    public bool DetailByPUContract { get; set; }

    public int RowVersion { get; set; }
    public string? MISACodeID { get; set; }

    // Navigation
    public Account? Parent { get; set; }
    public ICollection<Account> Children { get; set; } = [];
}
