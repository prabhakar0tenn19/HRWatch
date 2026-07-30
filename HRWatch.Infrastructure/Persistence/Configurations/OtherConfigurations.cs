using HRWatch.Domain.Entities;
using HRWatch.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRWatch.Infrastructure.Persistence.Configurations;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.RulesJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(p => p.ApplicableDepartment).HasMaxLength(150);
        builder.Property(p => p.CreatedBy).HasMaxLength(200);
        builder.Property(p => p.UpdatedBy).HasMaxLength(200);

        builder.HasIndex(p => p.IsActive);
    }
}

public class WeeklyReportConfiguration : IEntityTypeConfiguration<WeeklyReport>
{
    public void Configure(EntityTypeBuilder<WeeklyReport> builder)
    {
        builder.ToTable("WeeklyReports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        // Map DateRange value object as owned entity (two columns: PeriodStart, PeriodEnd)
        builder.OwnsOne(r => r.Period, periodBuilder =>
        {
            periodBuilder.Property(p => p.Start).HasColumnName("PeriodStart");
            periodBuilder.Property(p => p.End).HasColumnName("PeriodEnd");
        });

        builder.Property(r => r.ComplianceScore).HasPrecision(5, 2);
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.CreatedBy).HasMaxLength(200);
        builder.Property(r => r.UpdatedBy).HasMaxLength(200);

        builder.HasMany(r => r.Entries)
            .WithOne(e => e.WeeklyReport)
            .HasForeignKey(e => e.WeeklyReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.GeneratedAt);
    }
}

public class WeeklyReportEntryConfiguration : IEntityTypeConfiguration<WeeklyReportEntry>
{
    public void Configure(EntityTypeBuilder<WeeklyReportEntry> builder)
    {
        builder.ToTable("WeeklyReportEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.EmployeeFullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.EmployeeDepartment).HasMaxLength(150);
        builder.Property(e => e.ComplianceScore).HasPrecision(5, 2);
        builder.Property(e => e.TotalWorkHours).HasPrecision(7, 2);
        builder.Property(e => e.CreatedBy).HasMaxLength(200);
        builder.Property(e => e.UpdatedBy).HasMaxLength(200);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class ViolationConfiguration : IEntityTypeConfiguration<Violation>
{
    public void Configure(EntityTypeBuilder<Violation> builder)
    {
        builder.ToTable("Violations");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Type)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(v => v.Description).IsRequired().HasMaxLength(1000);
        builder.Property(v => v.AcknowledgedBy).HasMaxLength(200);
        builder.Property(v => v.CreatedBy).HasMaxLength(200);
        builder.Property(v => v.UpdatedBy).HasMaxLength(200);

        builder.HasOne(v => v.Employee)
            .WithMany(e => e.Violations)
            .HasForeignKey(v => v.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Policy)
            .WithMany()
            .HasForeignKey(v => v.PolicyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(v => v.EmployeeId);
        builder.HasIndex(v => v.OccurredOn);
        builder.HasIndex(v => v.IsAcknowledged);
    }
}

public class DesignationConfiguration : IEntityTypeConfiguration<Designation>
{
    public void Configure(EntityTypeBuilder<Designation> builder)
    {
        builder.ToTable("Designations");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Title).IsRequired().HasMaxLength(200);
        builder.Property(d => d.GradeLevel).HasMaxLength(20);
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.CreatedBy).HasMaxLength(200);
        builder.Property(d => d.UpdatedBy).HasMaxLength(200);
        builder.HasIndex(d => d.Title).IsUnique();
    }
}

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.Name).IsRequired().HasMaxLength(200);
        builder.Property(h => h.Region).HasMaxLength(100);
        builder.Property(h => h.ApplicableDepartment).HasMaxLength(150);
        builder.Property(h => h.CreatedBy).HasMaxLength(200);
        builder.Property(h => h.UpdatedBy).HasMaxLength(200);
        builder.HasIndex(h => h.Date);
    }
}
