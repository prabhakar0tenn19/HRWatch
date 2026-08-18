using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Enums;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Exceptions.Commands.RevokeException;

public record RevokeExceptionCommand(Guid ExceptionId) : ICommand<Result>;

public class RevokeExceptionCommandHandler : ICommandHandler<RevokeExceptionCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<RevokeExceptionCommandHandler> _logger;

    public RevokeExceptionCommandHandler(IApplicationDbContext dbContext, ILogger<RevokeExceptionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RevokeExceptionCommand command, CancellationToken cancellationToken = default)
    {
        var ex = await _dbContext.EmployeeExceptions.FirstOrDefaultAsync(e => e.Id == command.ExceptionId, cancellationToken);
        if (ex == null)
        {
            return Result.Failure("Exception record not found.", "NOT_FOUND");
        }

        ex.IsActive = false;
        ex.UpdatedAt = DateTime.UtcNow;
        _dbContext.EmployeeExceptions.Update(ex);

        // INSTANT ATTENDANCE RECONCILIATION ON REVOCATION:
        // Revert any DailyAttendance records that were marked 'E' (Exception) during this date range back to 'A' (Absent)
        var existingAttendances = await _dbContext.DailyAttendances
            .Where(a => a.EmployeeId == ex.EmployeeId &&
                        a.Date >= ex.FromDate &&
                        a.Date <= ex.ToDate &&
                        a.Status == AttendanceStatus.E)
            .ToListAsync(cancellationToken);

        foreach (var att in existingAttendances)
        {
            att.Status = AttendanceStatus.A;
            att.LeaveType = null;
            att.UpdatedAt = DateTime.UtcNow;
            _dbContext.DailyAttendances.Update(att);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Exception {Id} revoked for Employee {EmpId}. Reverted {Count} attendance records back to Absent.",
            command.ExceptionId, ex.EmployeeId, existingAttendances.Count);

        return Result.Success();
    }
}
