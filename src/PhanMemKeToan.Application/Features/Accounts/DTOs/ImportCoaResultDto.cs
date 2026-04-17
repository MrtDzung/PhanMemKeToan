namespace PhanMemKeToan.Application.Features.Accounts.DTOs;

public class ImportCoaResultDto
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Overwritten { get; set; }
    public List<string> Errors { get; set; } = [];
}
