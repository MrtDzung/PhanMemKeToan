using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Common.Models;

public record CompanyInfo(
    Guid TenantId,
    string Name,
    string Code,
    DatabaseMode DatabaseMode,
    TenantDbStatus DbStatus,
    string? DisplayRole,
    bool IsDefault
);
