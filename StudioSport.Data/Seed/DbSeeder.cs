using Microsoft.EntityFrameworkCore;
using StudioSport.Core.Entities;
using StudioSport.Core.Enums;
using BCrypt.Net;
namespace StudioSport.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync())
            return; // כבר מאותחל

        // --- משתמשות ---
        var admin = new AppUser
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.Admin
        };

        var member = new AppUser
        {
            Username = "ruthi",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Member123!"),
            Role = UserRole.Member
        };

        await db.Users.AddRangeAsync(admin, member);
        await db.SaveChangesAsync();

        // --- מנוי פעיל למשתמשת (חודש קדימה) ---
        await db.Subscriptions.AddAsync(new Subscription
        {
            UserId = member.Id,
            StartDate = DateTime.UtcNow.Date.AddDays(-1),
            EndDate = DateTime.UtcNow.Date.AddMonths(1),
            Status = SubscriptionStatus.Active
        });

        // --- מדריכות ---
        var trainer1 = new Trainer { Name = "שרה לוי", Specialty = "אימון כוח" };
        var trainer2 = new Trainer { Name = "מרים כהן", Specialty = "פילאטיס" };
        await db.Trainers.AddRangeAsync(trainer1, trainer2);
        await db.SaveChangesAsync();

        // --- שיעורים ---
        await db.ClassSessions.AddRangeAsync(
            new ClassSession
            {
                Name = "אימון כוח - בוקר",
                StartTime = DateTime.UtcNow.Date.AddDays(10).AddHours(8),
                Capacity = 5,
                EnrolledCount = 0,
                EquipmentNotes = "להביא מגבת ובקבוק מים",
                TrainerId = trainer1.Id
            },
            new ClassSession
            {
                Name = "פילאטיס - ערב",
                StartTime = DateTime.UtcNow.Date.AddDays(12).AddHours(18),
                Capacity = 2,
                EnrolledCount = 2, // מלא בכוונה, להדגמת 409
                EquipmentNotes = "להביא מזרן אישי",
                TrainerId = trainer2.Id
            });

        await db.SaveChangesAsync();
    }
}