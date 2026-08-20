using HRWatch.Application.Features.Employees.Queries.GetEmployeeById;
using HRWatch.Application.Features.Employees.Queries.GetEmployees;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IQueryMediator _queryMediator;

    public EmployeesController(IQueryMediator queryMediator)
    {
        _queryMediator = queryMediator;
    }

    /// <summary>
    /// Gets all active employees from the database with optional search and filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllEmployees(
        [FromQuery] string? searchTerm,
        [FromQuery] string? designation,
        [FromQuery] bool? isDeployed,
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _queryMediator.QueryAsync(
            new GetEmployeesQuery(searchTerm, designation, isDeployed, onlyActive),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets detailed information for an employee by Id.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEmployeeById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _queryMediator.QueryAsync(
            new GetEmployeeByIdQuery(id),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(result.Value);
    }
}
