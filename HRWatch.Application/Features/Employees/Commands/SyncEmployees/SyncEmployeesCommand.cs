using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Employees.DTOs;

namespace HRWatch.Application.Features.Employees.Commands.SyncEmployees;


public record SyncEmployeesCommand : ICommand<SyncEmployeesResult>
{
     public string? DepartmentFilter { get; init; }

      public bool ForceFullSync { get; init; } = false;

    public string TriggeredBy { get; init; } = "system";
}

public record SyncEmployeesResult(
    int EmployeesSynced,
    int EmployeesCreated,
    int EmployeesUpdated,
    DateTime SyncedAt);
