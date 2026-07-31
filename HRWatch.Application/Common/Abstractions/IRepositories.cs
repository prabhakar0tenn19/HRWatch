using HRWatch.Domain.Entities;
using HRWatch.Domain.ValueObjects;

namespace HRWatch.Application.Common.Abstractions;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default);
}

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<Employee?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetActiveEmployeesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string externalId, CancellationToken cancellationToken = default);
}

public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<IReadOnlyList<Attendance>> GetByEmployeeAndPeriodAsync(
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Attendance>> GetByDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<Attendance?> GetByEmployeeAndDateAsync(
        Guid employeeId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid employeeId,
        DateTime date,
        CancellationToken cancellationToken = default);
}

public interface IPolicyRepository : IRepository<Policy>
{
    Task<IReadOnlyList<Policy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Policy>> GetForDesignationAsync(Guid? designationId, string? department, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingActivePolicyAsync(Guid? designationId, DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludePolicyId = null, CancellationToken cancellationToken = default);
}

public interface IWeeklyReportRepository : IRepository<WeeklyReport>
{
    Task<WeeklyReport?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task<WeeklyReport?> GetByPeriodAsync(DateTime weekStart, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WeeklyReport>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}

public interface IViolationRepository : IRepository<Violation>
{
    Task<IReadOnlyList<Violation>> GetByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Violation>> GetByPeriodAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Violation>> GetUnacknowledgedAsync(CancellationToken cancellationToken = default);
}

public interface IHolidayRepository : IRepository<Holiday>
{
    Task<IReadOnlyList<Holiday>> GetForPeriodAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<bool> IsHolidayAsync(DateTime date, CancellationToken cancellationToken = default);
}

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityName, string entityId, CancellationToken cancellationToken = default);
}
