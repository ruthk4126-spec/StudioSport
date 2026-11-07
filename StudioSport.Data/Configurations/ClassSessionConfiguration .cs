using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioSport.Core.Entities;

namespace StudioSport.Data.Configurations;

public class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.EquipmentNotes).HasMaxLength(500);
        builder.Property(c => c.StartTime).IsRequired();

        // טוקן המקביליות
        builder.Property(c => c.Version).IsConcurrencyToken();

        builder.HasOne(c => c.Trainer)
            .WithMany(t => t.ClassSessions)
            .HasForeignKey(c => c.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}