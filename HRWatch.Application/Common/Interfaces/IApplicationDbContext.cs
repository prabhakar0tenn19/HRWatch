using HRWatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<DailyAttendance> DailyAttendances { get; }
    DbSet<EmployeeException> EmployeeExceptions { get; }
    DbSet<Policy> Policies { get; }
    DbSet<DailyPunchLog> DailyPunchLogs { get; }
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
