namespace HRWatch.Application.Features.Employees.DTOs;

public class ExternalEmployeeWeeklyOverviewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> Leave { get; set; } = [];
    public bool IsDeployed { get; set; }
}
