namespace PhanMemKeToan.Application.Features.AccountObjects.DTOs;

public class EmployeeProfileDto
{
    public Guid Id { get; set; }
    public string? CitizenId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? Gender { get; set; }
    public string? SocialInsuranceNumber { get; set; }
    public DateTime? HireDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int DependentCount { get; set; }
}
