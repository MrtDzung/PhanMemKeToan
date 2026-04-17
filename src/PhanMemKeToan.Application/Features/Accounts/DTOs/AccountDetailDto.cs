using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.Accounts.DTOs;

public class AccountDetailDto : AccountTreeNodeDto
{
    public Guid? ParentId { get; set; }
    public string? ParentNumber { get; set; }
    public string? ParentName { get; set; }

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
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
