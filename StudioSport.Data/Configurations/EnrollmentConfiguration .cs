using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioSport.Core.Entities;

namespace StudioSport.Data.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.CreatedAtUtc).IsRequired();

        // מונע רישום כפול לאותו שיעור
        builder.HasIndex(e => new { e.ClassSessionId, e.UserId }).IsUnique();

        builder.HasOne(e => e.ClassSession)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.ClassSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}