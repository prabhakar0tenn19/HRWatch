using HRWatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRWatch.Infrastructure.Persistence.Configurations;

/// <summary>
/// FLUENT API CONFIGURATION for the Employee entity.
/// 
/// This class tells EF Core HOW to map the Employee domain entity to the database.
/// 
/// WHY NOT DATA ANNOTATIONS ([Key], [Column], [Required])?
/// - Data Annotations pollute domain entities with infrastructure concerns
/// - Fluent API gives more power and flexibility
/// - Domain stays clean and framework-independent
/// - IEntityTypeConfiguration separates concerns cleanly
/// </summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        // ── Table ─────────────────────────────────────────────────────────────
        builder.ToTable("Employees");

        // ── Primary Key ───────────────────────────────────────────────────────
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever(); // We set ID in the entity

        // ── Required Properties ───────────────────────────────────────────────
        builder.Property(e => e.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Department)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Role)
            .HasConversion<string>()  // Store enum as string for readability in DB
            .HasMaxLength(50);

        // ── Optional Properties ───────────────────────────────────────────────
        builder.Property(e => e.PhoneNumber).HasMaxLength(20);
        builder.Property(e => e.CreatedBy).HasMaxLength(200);
        builder.Property(e => e.UpdatedBy).HasMaxLength(200);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(e => e.ExternalId).IsUnique();
        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.Department);

        // ── Relationships ─────────────────────────────────────────────────────
        builder.HasMany(e => e.AttendanceRecords)
            .WithOne(a => a.Employee)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Violations)
            .WithOne(v => v.Employee)
            .HasForeignKey(v => v.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Designation)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DesignationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Self-referencing: Manager → Employee relationship
        builder.HasOne(e => e.Manager)
            .WithMany()
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.NoAction); // avoid cascade cycle

        // Ignore domain events collection — not persisted to DB
        builder.Ignore(e => e.DomainEvents);
    }
}
