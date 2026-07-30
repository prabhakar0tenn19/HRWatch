using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Employees.DTOs;

namespace HRWatch.Application.Features.Employees.Queries.GetEmployees;


public record GetEmployeesQuery : IQuery<EmployeeListDto>
{
    
    public string? Department { get; init; }

   
    public bool ActiveOnly { get; init; } = true;

   
    public string? SearchTerm { get; init; }

    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
