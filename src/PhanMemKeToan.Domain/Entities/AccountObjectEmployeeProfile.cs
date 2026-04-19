using PhanMemKeToan.Domain.Common;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Domain.Entities;

/// <summary>
/// DD-007 — 1:1 extension of AccountObject for Employee type objects.
/// AccountObjectId serves as both PK and FK (shared primary key pattern).
/// </summary>
public class AccountObjectEmployeeProfile : AuditableEntity
{
    public Guid AccountObjectId { get; set; }   // PK + FK (1:1 — unique)
    public string? CitizenId { get; set; }      // max 20
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? SocialInsuranceNumber { get; set; } // max 10
    public DateTime? HireDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public int DependentCount { get; set; } = 0;

    // Navigation
    public AccountObject AccountObject { get; set; } = null!;
    public Department? Department { get; set; }
}
