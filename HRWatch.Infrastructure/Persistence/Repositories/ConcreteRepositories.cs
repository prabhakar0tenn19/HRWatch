using HRWatch.Application.Common.Abstractions;
using HRWatch.Domain.Entities;
using HRWatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Username == username || u.Email == email, cancellationToken);
    }
}

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Employee?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(e => e.Designation)
            .FirstOrDefaultAsync(e => e.ExternalId == externalId, cancellationToken);
    }

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetActiveEmployeesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Where(e => e.IsActive)
            .Include(e => e.Designation)
            .OrderBy(e => e.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Where(e => e.Department == department && e.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees.AnyAsync(e => e.ExternalId == externalId, cancellationToken);
    }

    public override async Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(e => e.Designation)
            .OrderBy(e => e.LastName)
            .ToListAsync(cancellationToken);
    }
}

public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Attendance>> GetByEmployeeAndPeriodAsync(
        Guid employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Attendance
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId && a.Date >= startDate.Date && a.Date <= endDate.Date)
            .OrderBy(a => a.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Attendance>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.Attendance
            .Include(a => a.Employee)
            .Where(a => a.Date == date.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<Attendance?> GetByEmployeeAndDateAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.Attendance
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == date.Date, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.Attendance
            .AnyAsync(a => a.EmployeeId == employeeId && a.Date == date.Date, cancellationToken);
    }
}

public class PolicyRepository : Repository<Policy>, IPolicyRepository
{
    public PolicyRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Policy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Policies
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Policy>> GetForDepartmentAsync(string department, CancellationToken cancellationToken = default)
    {
        return await _context.Policies
            .Where(p => p.IsActive && (p.ApplicableDepartment == null || p.ApplicableDepartment == department))
            .ToListAsync(cancellationToken);
    }
}

public class WeeklyReportRepository : Repository<WeeklyReport>, IWeeklyReportRepository
{
    public WeeklyReportRepository(ApplicationDbContext context) : base(context) { }

    public async Task<WeeklyReport?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WeeklyReports
            .Include(r => r.Entries)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<WeeklyReport?> GetByPeriodAsync(DateTime weekStart, CancellationToken cancellationToken = default)
    {
        return await _context.WeeklyReports
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.Period.Start == weekStart.Date, cancellationToken);
    }

    public async Task<IReadOnlyList<WeeklyReport>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.WeeklyReports
            .OrderByDescending(r => r.GeneratedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}

public class ViolationRepository : Repository<Violation>, IViolationRepository
{
    public ViolationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Violation>> GetByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.Violations
            .Where(v => v.EmployeeId == employeeId)
            .OrderByDescending(v => v.OccurredOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Violation>> GetByPeriodAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        return await _context.Violations
            .Where(v => v.OccurredOn >= start && v.OccurredOn <= end)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Violation>> GetUnacknowledgedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Violations
            .Where(v => !v.IsAcknowledged)
            .Include(v => v.Employee)
            .ToListAsync(cancellationToken);
    }
}

public class HolidayRepository : Repository<Holiday>, IHolidayRepository
{
    public HolidayRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Holiday>> GetForPeriodAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .Where(h => h.Date >= start.Date && h.Date <= end.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsHolidayAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.Holidays.AnyAsync(h => h.Date == date.Date, cancellationToken);
    }
}
