using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<DailyAttendance> DailyAttendances => Set<DailyAttendance>();
    public DbSet<EmployeeException> EmployeeExceptions => Set<EmployeeException>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<DailyPunchLog> DailyPunchLogs => Set<DailyPunchLog>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
