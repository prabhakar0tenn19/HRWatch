using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Employees.DTOs;

namespace HRWatch.Application.Features.Employees.Queries.GetEmployeeById{
public record GetEmployeeByIdQuery(Guid Id) : IQuery<EmployeeDto>;


}


