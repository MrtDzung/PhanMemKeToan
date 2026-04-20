namespace PhanMemKeToan.Domain.Common.Exceptions;

public class RoleInUseException(IReadOnlyList<string> affectedUserNames)
    : Exception($"Role is assigned to {affectedUserNames.Count} user(s) and cannot be deleted.")
{
    public IReadOnlyList<string> AffectedUserNames { get; } = affectedUserNames;
}
