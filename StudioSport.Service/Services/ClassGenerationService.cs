using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudioSport.Core.Entities;
using StudioSport.Data;

namespace StudioSport.Service.Services;

public class ClassGenerationService : BackgroundService
{
    private const int WeeksAhead = 8;
    private static readonly TimeZoneInfo StudioTz =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClassGenerationService> _logger;

    public ClassGenerationService(
        IServiceScopeFactory scopeFactory,
        ILogger<ClassGenerationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // הרצה בעלייה — משלימה פערים ומכסה את השבוע הקרוב
        await RunOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeUntilNextRun(), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break; // עצירה מסודרת
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            await GenerateAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // כיבוי באמצע — לא לוגים
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Class generation failed");
        }
    }

    /// <summary>הרצה שבועית: יום ראשון 03:00 UTC.</summary>
    private static TimeSpan TimeUntilNextRun()
    {
        var now = DateTime.UtcNow;
        var next = now.Date.Add(new TimeSpan(3, 0, 0));

        while (next.DayOfWeek != DayOfWeek.Sunday)
            next = next.AddDays(1);

        if (next <= now)
            next = next.AddDays(7);

        return next - now;
    }

    private async Task GenerateAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var templates = await db.ClassTemplates
            .Where(t => t.IsActive)
            .ToListAsync(ct);

        if (templates.Count == 0)
        {
            _logger.LogInformation("Class generation skipped: no active templates");
            return;
        }

        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, StudioTz));

        var horizon = today.AddDays(WeeksAhead * 7);
        var created = 0;

        foreach (var t in templates)
        {
            // שולפים פעם אחת את כל המועדים הקיימים של התבנית —
            // כולל מבוטלים, כדי לא לשחזר שיעור שבוטל
            var existing = (await db.ClassSessions
                .Where(c => c.ClassTemplateId == t.Id)
                .Select(c => c.StartTime)
                .ToListAsync(ct))
                .ToHashSet();

            for (var d = today; d <= horizon; d = d.AddDays(1))
            {
                if (d.DayOfWeek != t.DayOfWeek) continue;

                var local = DateTime.SpecifyKind(d.ToDateTime(t.StartTime), DateTimeKind.Unspecified);
                var startUtc = TimeZoneInfo.ConvertTimeToUtc(local, StudioTz);

                // לא יוצרים מופע שכבר התקיים
                if (startUtc <= DateTime.UtcNow) continue;

                // דילוג לפי זוג (תבנית, מועד) בלבד
                if (existing.Contains(startUtc)) continue;

                db.ClassSessions.Add(new ClassSession
                {
                    Name = t.Name,
                    StartTime = startUtc,
                    Capacity = t.Capacity,
                    EnrolledCount = 0,
                    EquipmentNotes = t.EquipmentNotes,
                    IsCancelled = false,
                    TrainerId = t.TrainerId,
                    ClassTemplateId = t.Id,
                    Version = Guid.NewGuid()
                });

                created++;
            }

            t.GeneratedUntil = horizon;
        }

        if (created > 0)
            await db.SaveChangesAsync(ct);

        _logger.LogInformation("Class generation finished: {Count} sessions created", created);
    }
}