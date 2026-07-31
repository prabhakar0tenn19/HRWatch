namespace HRWatch.Application.Features.Employees.DTOs;

public record EmployeeDto
{
    public Guid Id { get; init; }
    public string ExternalId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Designation { get; init; } = string.Empty;
    public DateTime JoinDate { get; init; }
    public bool IsActive { get; init; }
}

public record EmployeeListDto(
    IReadOnlyList<EmployeeDto> Employees,
    int TotalCount,
    int Page,
    int PageSize);

public record ExternalEmployeeDto
{
    public string ExternalId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Designation { get; init; } = string.Empty;
    public DateTime JoinDate { get; init; }
    public bool IsActive { get; init; } = true;
}
