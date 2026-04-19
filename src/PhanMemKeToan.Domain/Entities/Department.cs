using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

public class Department : AuditableEntity
{
    public string DepartmentCode { get; set; } = string.Empty; // max 25
    public string DepartmentName { get; set; } = string.Empty; // max 255
    public Guid? ParentId { get; set; }
    public int Level { get; set; } = 1;                        // 1-5, computed on write
    public bool IsActive { get; set; } = true;

    // Self-referential navigation
    public Department? Parent { get; set; }
    public ICollection<Department> Children { get; set; } = [];

    // Navigation
    public ICollection<AccountObjectEmployeeProfile> EmployeeProfiles { get; set; } = [];
}
