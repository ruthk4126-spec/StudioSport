using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioSport.Core.Entities;

namespace StudioSport.Data.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Comment).HasMaxLength(500);

        // משוב אחד לכל רישום - זה גם הקשר one-to-one
        builder.HasOne(f => f.Enrollment)
            .WithOne()
            .HasForeignKey<Feedback>(f => f.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.EnrollmentId).IsUnique();
    }
}