using Microsoft.EntityFrameworkCore;
using StudioSport.Core.Entities;
using StudioSport.Data;
using Xunit;

namespace StudioSport.Tests.Integration;

public class EnrollmentConcurrencyTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("STUDIO_TEST_DB")
        ?? throw new InvalidOperationException("Set STUDIO_TEST_DB.");

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    [Fact]
    public async Task TwoContexts_SameEntity_SecondThrowsConcurrencyException()
    {
        int classId, trainerId;

        // Arrange
        await using (var setup = CreateContext())
        {
            var trainer = new Trainer { Name = "מאמנת בדיקה" };
            setup.Trainers.Add(trainer);
            await setup.SaveChangesAsync();
            trainerId = trainer.Id;

            var session = new ClassSession
            {
                Name = "שיעור בדיקה",
                StartTime = DateTime.UtcNow.AddDays(30),
                Capacity = 1,
                EnrolledCount = 0,
                TrainerId = trainerId,
                Version = Guid.NewGuid()
            };
            setup.ClassSessions.Add(session);
            await setup.SaveChangesAsync();
            classId = session.Id;
        }

        try
        {
            // שני הקשרים קוראים את אותה ישות
            await using var ctx1 = CreateContext();
            await using var ctx2 = CreateContext();

            var s1 = await ctx1.ClassSessions.FirstAsync(c => c.Id == classId);
            var s2 = await ctx2.ClassSessions.FirstAsync(c => c.Id == classId);

            s1.EnrolledCount++;
            s1.Version = Guid.NewGuid();

            s2.EnrolledCount++;
            s2.Version = Guid.NewGuid();

            // Act + Assert
            await ctx1.SaveChangesAsync();   // הראשון מצליח

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => ctx2.SaveChangesAsync());   // השני נופל
        }
        finally
        {
            await using var cleanup = CreateContext();
            await cleanup.ClassSessions.Where(c => c.Id == classId).ExecuteDeleteAsync();
            await cleanup.Trainers.Where(t => t.Id == trainerId).ExecuteDeleteAsync();
        }
    }
}