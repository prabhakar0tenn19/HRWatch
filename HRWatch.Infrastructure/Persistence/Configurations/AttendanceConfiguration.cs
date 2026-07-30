using HRWatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRWatch.Infrastructure.Persistence.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendance");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.TotalWorkHours)
            .HasPrecision(5, 2);

        builder.Property(a => a.ExternalReferenceId).HasMaxLength(200);
        builder.Property(a => a.Remarks).HasMaxLength(500);
        builder.Property(a => a.CreatedBy).HasMaxLength(200);
        builder.Property(a => a.UpdatedBy).HasMaxLength(200);

        // Unique: one record per employee per day
        builder.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
        builder.HasIndex(a => a.Date);

        builder.HasOne(a => a.Employee)
            .WithMany(e => e.AttendanceRecords)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
