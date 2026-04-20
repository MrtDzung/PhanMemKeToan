namespace PhanMemKeToan.Domain.Enums;

/// <summary>
/// Bitmask flags for AccountObject type classification.
/// Values can be combined: e.g., Customer|Vendor = 3.
/// </summary>
public static class ObjectType
{
    public const int Customer = 1;
    public const int Vendor = 2;
    public const int Employee = 4;

    /// <summary>Returns true if flags is a valid non-zero combination (1–7).</summary>
    public static bool IsValid(int flags) => flags >= 1 && flags <= 7;
}
