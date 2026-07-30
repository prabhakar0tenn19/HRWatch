using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Attendance.DTOs;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Attendance.Queries.GetAttendance;

public class GetAttendanceQueryHandler : IQueryHandler<GetAttendanceQuery, IReadOnlyList<AttendanceDto>>
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository   _employeeRepository;
    private readonly ILogger<GetAttendanceQueryHandler> _logger;

    public GetAttendanceQueryHandler(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository   employeeRepository,
        ILogger<GetAttendanceQueryHandler> logger)
    {
        _attendanceRepository = attendanceRepository;
        _employeeRepository   = employeeRepository;
        _logger               = logger;
    }

    public async Task<Result<IReadOnlyList<AttendanceDto>>> HandleAsync(
        GetAttendanceQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Domain.Entities.Attendance> records;

        if (query.EmployeeId.HasValue)
        {
            records = await _attendanceRepository.GetByEmployeeAndPeriodAsync(
                query.EmployeeId.Value, query.FromDate, query.ToDate, cancellationToken);
        }
        else
        {
            records = await _attendanceRepository.GetByDateAsync(query.FromDate, cancellationToken);
        }

        var dtos = records.Select(r => new AttendanceDto
        {
            Id             = r.Id,
            EmployeeId     = r.EmployeeId,
            EmployeeName   = r.Employee?.FullName ?? "Unknown",
            Date           = r.Date,
            CheckIn        = r.CheckIn,
            CheckOut       = r.CheckOut,
            Status         = r.Status,
            StatusDisplay  = r.Status.ToString(),
            TotalWorkHours = r.TotalWorkHours,
            Remarks        = r.Remarks
        }).ToList();

        return Result<IReadOnlyList<AttendanceDto>>.Success(dtos);
    }
}
