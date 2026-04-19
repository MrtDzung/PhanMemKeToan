using PhanMemKeToan.Domain.Common;

namespace PhanMemKeToan.Domain.Entities;

/// <summary>
/// CRUD deferred to later sprint (BR-E). FK already present on AccountObject.
/// </summary>
public class AccountObjectGroup : AuditableEntity
{
    public string GroupCode { get; set; } = string.Empty;   // max 25
    public string GroupName { get; set; } = string.Empty;   // max 255
    public int ObjectType { get; set; }                     // bitmask — see ObjectType static class
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<AccountObject> AccountObjects { get; set; } = [];
}
