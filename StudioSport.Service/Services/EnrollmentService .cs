using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioSport.Core.Common;
using StudioSport.Core.DTOs.Enrollments;
using StudioSport.Core.Entities;
using StudioSport.Core.Enums;
using StudioSport.Core.Interfaces;

namespace StudioSport.Service.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollments;
    private readonly IClassRepository _classes;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IFeedbackRepository _feedbacks;
    private readonly ILogger<EnrollmentService> _logger;
    private readonly BookingSettings _settings;
    private readonly ISubscriptionService _subscriptionService;

    public EnrollmentService(
        IEnrollmentRepository enrollments,
        IClassRepository classes,
        ISubscriptionRepository subscriptions,
        IFeedbackRepository feedbacks,
        ILogger<EnrollmentService> logger,
        IOptions<BookingSettings> settings,
        ISubscriptionService subscriptionService)
    {
        _enrollments = enrollments;
        _classes = classes;
        _subscriptions = subscriptions;
        _subscriptionService = subscriptionService;
        _feedbacks = feedbacks;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<EnrollmentResult> EnrollAsync(int classId, int userId, CancellationToken ct)
    {
        // 1. השיעור קיים?
        var session = await _classes.GetByIdAsync(classId, ct);
        if (session is null)
            return EnrollmentResult.Failure(EnrollmentOutcome.ClassNotFound);

        // 2. השיעור בוטל?
        if (session.IsCancelled)
            return EnrollmentResult.Failure(EnrollmentOutcome.ClassCancelled);

        // 3. השיעור כבר התקיים?
        if (session.StartTime <= DateTime.UtcNow)
            return EnrollmentResult.Failure(EnrollmentOutcome.ClassAlreadyStarted);

        // 4. חלון הרישום פתוח?
        var minAllowed = DateTime.UtcNow.AddDays(_settings.MinDaysBeforeClass);
        if (session.StartTime < minAllowed)
            return EnrollmentResult.Failure(EnrollmentOutcome.BookingWindowClosed);

        // 5. יש מנוי פעיל שמכסה את מועד השיעור?
        var subscription = await _subscriptions
            .GetActiveByUserIdAsync(userId, session.StartTime, ct);
        if (subscription is null)
            return EnrollmentResult.Failure(EnrollmentOutcome.SubscriptionRequired);

        // 5ב. יש יתרת שיעורים במנוי? (אין ערך נפרד ב-enum — אותה תוצאה כמו "אין מנוי")
        if (subscription.TotalClasses <= subscription.UsedClasses)
        {
            _logger.LogInformation(
                "User {UserId} has no credit left: {Used}/{Total}",
                userId, subscription.UsedClasses, subscription.TotalClasses);

            return EnrollmentResult.Failure(EnrollmentOutcome.SubscriptionRequired);
        }

        // 6. כבר רשומה?
        var existingActive = await _enrollments.GetByUserAndClassAsync(userId, classId, ct);
        if (existingActive is not null)
            return EnrollmentResult.Failure(EnrollmentOutcome.AlreadyEnrolled);

        // רשומה שבוטלה באותו שיעור: מחדשים אותה במקום ליצור חדשה.
        // האינדקס הייחודי (ClassSessionId, UserId) חל גם על רשומות מבוטלות,
        // ולכן INSERT חדש היה נכשל למרות שהרשומה הקודמת אינה פעילה.
        var previous = await _enrollments.GetLatestByUserAndClassAsync(userId, classId, ct);

        // 7. מכסת שיעורים ליום
        if (_settings.MaxClassesPerDay > 0)
        {
            var todayCount = await _enrollments
                .CountUserEnrollmentsOnDateAsync(userId, session.StartTime, ct);

            if (todayCount >= _settings.MaxClassesPerDay)
                return EnrollmentResult.Failure(EnrollmentOutcome.DailyLimitReached);
        }

        // 8. תפיסת מקום אטומית - הלב
        var reserved = await _classes.TryReserveSpotAsync(classId, ct);
        if (!reserved)
        {
            _logger.LogWarning(
                "Booking conflict: class {ClassId} is full. User {UserId} was rejected.",
                classId, userId);

            return EnrollmentResult.Failure(EnrollmentOutcome.ClassFull);
        }

        // 9. יצירת הרישום + צריכת השיעור, עם שחרור המקום בכל כשלון
        try
        {
            Enrollment enrollment;

            if (previous is not null)
            {
                // החזרת רשומה מבוטלת לשימוש — שומרת על ההיסטוריה ומכבדת את האינדקס
                previous.Status = EnrollmentStatus.Active;
                previous.CreatedAtUtc = DateTime.UtcNow;
                previous.CancelledAtUtc = null;
                await _enrollments.UpdateAsync(previous, ct);
                enrollment = previous;
            }
            else
            {
                enrollment = new Enrollment
                {
                    ClassSessionId = classId,
                    UserId = userId,
                    Status = EnrollmentStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _enrollments.AddAsync(enrollment, ct);
            }

            await _subscriptionService.ConsumeCreditAsync(userId, ct);

            _logger.LogInformation(
                "User {UserId} enrolled in class {ClassId}. EnrollmentId {EnrollmentId}",
                userId, classId, enrollment.Id);

            return EnrollmentResult.Success(enrollment.Id);
        }
        catch
        {
            // מחזירים את המקום שנתפס, כדי שלא יישאר "תפוס" בלי רישום
            await _classes.ReleaseSpotAsync(classId, ct);
            throw;
        }
    }

    public async Task<bool> CancelAsync(int enrollmentId, int userId, CancellationToken ct)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId, ct);
        if (enrollment is null)
            return false;

        // רק הבעלים או מנהלת - הבדיקה מתבצעת ב-Controller עבור Admin
        if (enrollment.UserId != userId)
            return false;

        if (enrollment.Status != EnrollmentStatus.Active)
            return false;

        enrollment.Status = EnrollmentStatus.Cancelled;
        enrollment.CancelledAtUtc = DateTime.UtcNow;
        await _enrollments.UpdateAsync(enrollment, ct);

        // שחרור המקום והחזרת השיעור למכסה
        await _classes.ReleaseSpotAsync(enrollment.ClassSessionId, ct);
        await _subscriptionService.RefundCreditAsync(userId, ct);

        _logger.LogInformation(
            "Enrollment {EnrollmentId} cancelled by user {UserId}",
            enrollmentId, userId);

        return true;
    }

    public async Task<MyEnrollmentsDto> GetMyEnrollmentsAsync(int userId, CancellationToken ct)
    {
        var list = await _enrollments.GetUserEnrollmentsAsync(userId, ct);
        var now = DateTime.UtcNow;

        // שאילתה אחת לכל המשובים — ולא בדיקה לכל רישום (N+1)
        var withFeedback = await _feedbacks
            .GetEnrollmentIdsWithFeedbackAsync(list.Select(e => e.Id), ct);

        var mapped = list.Select(e =>
        {
            var startTime = e.ClassSession?.StartTime ?? default;

            return new EnrollmentResponseDto
            {
                Id = e.Id,
                ClassId = e.ClassSessionId,
                ClassName = e.ClassSession?.Name ?? string.Empty,
                StartTime = startTime,
                Status = e.Status.ToString(),
                IsPast = startTime <= now,
                CreatedAtUtc = e.CreatedAtUtc,
                EquipmentNotes = e.ClassSession?.EquipmentNotes,
                HasFeedback = withFeedback.Contains(e.Id)
            };
        }).ToList();

        // "קרוב" = הרשמה פעילה שהשיעור שלה טרם התקיים
        var active = mapped
            .Where(e => e.Status == nameof(EnrollmentStatus.Active) && !e.IsPast)
            .OrderBy(e => e.StartTime)
            .ToList();

        var history = mapped
            .Where(e => e.Status != nameof(EnrollmentStatus.Active) || e.IsPast)
            .OrderByDescending(e => e.StartTime)
            .ToList();

        return new MyEnrollmentsDto { Active = active, History = history };
    }

    public async Task<EnrollmentResult> AddFeedbackAsync(
        int enrollmentId, int userId, FeedbackCreateDto dto, CancellationToken ct)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId, ct);

        if (enrollment is null || enrollment.UserId != userId)
            return EnrollmentResult.Failure(EnrollmentOutcome.ClassNotFound);

        if (enrollment.Status != EnrollmentStatus.Active)
            return EnrollmentResult.Failure(EnrollmentOutcome.FeedbackNotAllowed);

        // משוב רק אחרי שהשיעור התקיים
        if (enrollment.ClassSession is null || enrollment.ClassSession.StartTime > DateTime.UtcNow)
            return EnrollmentResult.Failure(EnrollmentOutcome.FeedbackNotAllowed);

        var already = await _feedbacks.GetByEnrollmentIdAsync(enrollmentId, ct);
        if (already is not null)
            return EnrollmentResult.Failure(EnrollmentOutcome.FeedbackAlreadyExists);

        await _feedbacks.AddAsync(new Feedback
        {
            EnrollmentId = enrollmentId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

        return EnrollmentResult.Success(enrollmentId);
    }
}