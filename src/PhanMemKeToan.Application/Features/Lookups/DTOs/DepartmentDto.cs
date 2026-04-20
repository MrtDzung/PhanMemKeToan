namespace PhanMemKeToan.Application.Features.Lookups.DTOs;

public record DepartmentDto(
    Guid Id,
    string DepartmentCode,
    string DepartmentName,
    Guid? ParentId,
    int Level,
    bool IsActive);
