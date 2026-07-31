using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Employees.DTOs;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Employees.Queries.GetEmployeeById{

public class GetEmployeeByIdQueryHandler : IQueryHandler<GetEmployeeByIdQuery, EmployeeDto>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<GetEmployeeByIdQueryHandler> _logger;

    public GetEmployeeByIdQueryHandler(
        IEmployeeRepository employeeRepository,
        ILogger<GetEmployeeByIdQueryHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<Result<EmployeeDto>> HandleAsync(
        GetEmployeeByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting employee details for Id: {EmployeeId}", query.Id);

        var employee = await _employeeRepository.GetByIdAsync(query.Id, cancellationToken);

        if (employee is null)
        {
            return Result<EmployeeDto>.Failure(
                new Error("NOT_FOUND", $"Employee with Id '{query.Id}' was not found."));
        }

        var dto = new EmployeeDto
        {
            Id = employee.Id,
            ExternalId = employee.ExternalId,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            FullName = employee.FullName,
            Email = employee.Email,
            Department = employee.Department,
            Designation = employee.Designation?.Title ?? string.Empty,
            JoinDate = employee.JoinDate,
            IsActive = employee.IsActive
        };

        return Result<EmployeeDto>.Success(dto);
    }
}
}