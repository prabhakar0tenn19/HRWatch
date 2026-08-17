using HRWatch.Domain.Entities;
using HRWatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRWatch.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.EmployeeCode)
            .IsUnique();

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(e => e.Email)
            .IsUnique();

        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Designation)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Location)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("India");

        builder.Property(e => e.IsDeployed)
            .HasDefaultValue(true);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(e => new { e.IsActive, e.Location });
    }
}

public class DailyAttendanceConfiguration : IEntityTypeConfiguration<DailyAttendance>
{
    public void Configure(EntityTypeBuilder<DailyAttendance> builder)
    {
        builder.ToTable("DailyAttendance");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(a => a.LeaveType)
            .HasMaxLength(50);

        builder.HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique();

        builder.HasIndex(a => new { a.Date, a.Status });

        builder.HasOne(a => a.Employee)
            .WithMany(e => e.Attendances)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Policy)
            .WithMany(p => p.Attendances)
            .HasForeignKey(a => a.RuleVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeExceptionConfiguration : IEntityTypeConfiguration<EmployeeException>
{
    public void Configure(EntityTypeBuilder<EmployeeException> builder)
    {
        builder.ToTable("EmployeeExceptions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(e => new { e.EmployeeId, e.FromDate, e.ToDate, e.IsActive });

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.Exceptions)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PolicyName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.RulesJson)
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.IsActive);
    }
}

public class DailyPunchLogConfiguration : IEntityTypeConfiguration<DailyPunchLog>
{
    public void Configure(EntityTypeBuilder<DailyPunchLog> builder)
    {
        builder.ToTable("DailyPunchLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.EmployeeCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.DeviceName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.RawLogIndex)
            .HasMaxLength(50);

        builder.HasIndex(l => new { l.PunchDate, l.EmployeeCode });

        builder.HasOne(l => l.Employee)
            .WithMany()
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);
    }
}
