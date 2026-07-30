using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Employees.DTOs;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Employees.Queries.GetEmployees;


public class GetEmployeesQueryHandler : IQueryHandler<GetEmployeesQuery, EmployeeListDto>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<GetEmployeesQueryHandler> _logger;

    public GetEmployeesQueryHandler(
        IEmployeeRepository employeeRepository,
        ILogger<GetEmployeesQueryHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _logger             = logger;
    }

    public async Task<Result<EmployeeListDto>> HandleAsync(
        GetEmployeesQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting employees. Department: {Department}, ActiveOnly: {ActiveOnly}",
            query.Department, query.ActiveOnly);

        var employees = query.ActiveOnly
            ? await _employeeRepository.GetActiveEmployeesAsync(cancellationToken)
            : await _employeeRepository.GetAllAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(query.Department))
            employees = employees
                .Where(e => e.Department.Equals(query.Department, StringComparison.OrdinalIgnoreCase))
                .ToList();

       
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLowerInvariant();
            employees = employees
                .Where(e => e.FullName.ToLowerInvariant().Contains(term)
                         || e.Email.ToLowerInvariant().Contains(term))
                .ToList();
        }

        // Paging
        var totalCount = employees.Count;
        var paged      = employees
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        // Map entities → DTOs
        var dtos = paged.Select(e => new EmployeeDto
        {
            Id          = e.Id,
            ExternalId  = e.ExternalId,
            FirstName   = e.FirstName,
            LastName    = e.LastName,
            FullName    = e.FullName,
            Email       = e.Email,
            Department  = e.Department,
            Role        = e.Role.ToString(),
            Designation = e.Designation?.Title ?? string.Empty,
            JoinDate    = e.JoinDate,
            IsActive    = e.IsActive
        }).ToList();

        return Result<EmployeeListDto>.Success(
            new EmployeeListDto(dtos, totalCount, query.Page, query.PageSize));
    }
}
