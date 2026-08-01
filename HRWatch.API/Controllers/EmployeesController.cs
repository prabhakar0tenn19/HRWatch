using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Employees.Commands.SyncEmployees;
using HRWatch.Application.Features.Employees.Queries.GetEmployees;
using Microsoft.AspNetCore.Mvc;
using HRWatch.Application.Features.Employees.Queries.GetEmployeeById;
using HRWatch.Application.Features.Employees.Commands.CreateEmployee;
using HRWatch.Application.Features.Employees.Commands.UpdateEmployee;


namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly ICommandMediator _commandMediator;
    private readonly IQueryMediator _queryMediator;

    public EmployeesController(ICommandMediator commandMediator, IQueryMediator queryMediator)
    {
        _commandMediator = commandMediator;
        _queryMediator = queryMediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? department = null,
        [FromQuery] bool activeOnly = true,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmployeesQuery
        {
            Department = department,
            ActiveOnly = activeOnly,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize
        };

        var result = await _queryMediator.SendAsync(query, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error.Message, code = result.Error.Code });
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncEmployees(
        [FromBody] SyncEmployeesCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _commandMediator.SendAsync(command, cancellationToken);
        return result.IsSuccess
            ? Ok(new { message = "Sync completed.", data = result.Value })
            : BadRequest(new { error = result.Error.Message, code = result.Error.Code });
    }



    [HttpGet("{id:guid}")]
public async Task<IActionResult> GetEmployeeById(
    [FromRoute] Guid id,
    CancellationToken cancellationToken = default)
{
    var query = new GetEmployeeByIdQuery(id);
    var result = await _queryMediator.SendAsync(query, cancellationToken);

    return result.IsSuccess
        ? Ok(result.Value)
        : NotFound(new { error = result.Error.Message, code = result.Error.Code });
}



[HttpPost]
public async Task<IActionResult> CreateEmployee(
    [FromBody] CreateEmployeeCommand command,
    CancellationToken cancellationToken = default)
{
    var result = await _commandMediator.SendAsync(command, cancellationToken);

    return result.IsSuccess
    ? CreatedAtAction(
        nameof(GetEmployeeById),
        new { id = result.Value },
        result.Value)
    : BadRequest(new
    {
        error = result.Error.Message,
        code = result.Error.Code
    });
}

[HttpPut("{id:guid}")]
public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeCommand command)
{
    if (id != command.Id)
    {
        return BadRequest(new { error = "Mismatched Employee ID in URL and body." });
    }

    var result = await _commandMediator.SendAsync(command);

    return result.IsSuccess
        ? NoContent()
        : BadRequest(new { error = result.Error.Message, code = result.Error.Code });
}



}
