using Microsoft.EntityFrameworkCore;
using StudioSport.Core.Entities;
using StudioSport.Data;
using Xunit;

namespace StudioSport.Tests.Integration;

/// בדיקות אינטגרציה שדורשות בסיס נתונים אמיתי.
/// מריצים אותן כך (דוגמה ל-PostgreSQL מקומי):
///   STUDIO_TEST_DB="Host=localhost;Database=StudioSportTest;Username=postgres;Password=..." dotnet test
/// בהיעדר המשתנה הן מדווחות Skip — ולא "עוברות" בלי לבדוק כלום.
public class EnrollmentConcurrencyTests
{
    private static string? ConnectionString =>
          Environment.GetEnvironmentVariable("STUDIO_TEST_DB");

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    [SkippableFact]
    public async Task TwoContexts_SameEntity_SecondThrowsConcurrencyException()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString),
            "חסר משתנה הסביבה STUDIO_TEST_DB — הבדיקה דורשת בסיס נתונים אמיתי.");

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

    /// ההוכחה שלא נשאר מקום "כפול": על שיעור בקיבולת 1, ניסיון תפיסה שני
    /// (אחרי שהראשון הצליח) מוחזר false, והמונה לא עולה מעל הקיבולת.
    [SkippableFact]
    public async Task ReserveSpot_SecondAttemptOnFullClass_ReturnsFalseAndKeepsCount()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString),
            "חסר משתנה הסביבה STUDIO_TEST_DB — הבדיקה דורשת בסיס נתונים אמיתי.");

        int classId, trainerId;

        await using (var setup = CreateContext())
        {
            var trainer = new Trainer { Name = "מאמנת בדיקה" };
            setup.Trainers.Add(trainer);
            await setup.SaveChangesAsync();
            trainerId = trainer.Id;

            var session = new ClassSession
            {
                Name = "שיעור בקיבולת אחת",
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
            var repo1 = new StudioSport.Data.Repositories.ClassRepository(CreateContext());
            var repo2 = new StudioSport.Data.Repositories.ClassRepository(CreateContext());

            var first = await repo1.TryReserveSpotAsync(classId, CancellationToken.None);
            var second = await repo2.TryReserveSpotAsync(classId, CancellationToken.None);

            Assert.True(first, "המקום הראשון אמור להיתפס");
            Assert.False(second, "המקום השני לא אמור להיתפס — הקיבולת 1");

            await using var check = CreateContext();
            var final = await check.ClassSessions.AsNoTracking().FirstAsync(c => c.Id == classId);
            Assert.Equal(1, final.EnrolledCount);
        }
        finally
        {
            await using var cleanup = CreateContext();
            await cleanup.ClassSessions.Where(c => c.Id == classId).ExecuteDeleteAsync();
            await cleanup.Trainers.Where(t => t.Id == trainerId).ExecuteDeleteAsync();
        }
    }
}