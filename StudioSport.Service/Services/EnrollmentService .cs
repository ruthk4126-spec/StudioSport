using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioSport.Core.Common;
using StudioSport.Core.DTOs.Enrollments;
using StudioSport.Core.Entities;
using StudioSport.Core.Enums;
using StudioSport.Core.Interfaces;
using StudioSport.Core.Common;

namespace StudioSport.Service.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollments;
    private readonly IClassRepository _classes;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IFeedbackRepository _feedbacks;
    private readonly ILogger<EnrollmentService> _logger;
    private readonly BookingSettings _settings;

    public EnrollmentService(
        IEnrollmentRepository enrollments,
        IClassRepository classes,
        ISubscriptionRepository subscriptions,
        IFeedbackRepository feedbacks,
        ILogger<EnrollmentService> logger,
        IOptions<BookingSettings> settings)
    {
        _enrollments = enrollments;
        _classes = classes;
        _subscriptions = subscriptions;
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

        // 6. כבר רשומה?
        var existing = await _enrollments.GetByUserAndClassAsync(userId, classId, ct);
        if (existing is not null)
            return EnrollmentResult.Failure(EnrollmentOutcome.AlreadyEnrolled);

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

        // 9. יצירת הרישום
        var enrollment = new Enrollment
        {
            ClassSessionId = classId,
            UserId = userId,
            Status = EnrollmentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _enrollments.AddAsync(enrollment, ct);

        _logger.LogInformation(
            "User {UserId} enrolled in class {ClassId}. EnrollmentId {EnrollmentId}",
            userId, classId, enrollment.Id);

        return EnrollmentResult.Success(enrollment.Id);
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

        // שחרור המקום
        await _classes.ReleaseSpotAsync(enrollment.ClassSessionId, ct);

        _logger.LogInformation(
            "Enrollment {EnrollmentId} cancelled by user {UserId}",
            enrollmentId, userId);

        return true;
    }

    public async Task<List<EnrollmentResponseDto>> GetMyEnrollmentsAsync(int userId, CancellationToken ct)
    {
        var list = await _enrollments.GetUserEnrollmentsAsync(userId, ct);

        return list.Select(e => new EnrollmentResponseDto
        {
            Id = e.Id,
            ClassId = e.ClassSessionId,
            ClassName = e.ClassSession?.Name ?? string.Empty,
            StartTime = e.ClassSession?.StartTime ?? default,
            Status = e.Status.ToString(),
            CreatedAtUtc = e.CreatedAtUtc,
            EquipmentNotes = e.ClassSession?.EquipmentNotes
        }).ToList();
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
            return EnrollmentResult.Failure(EnrollmentOutcome.FeedbackNotAllowed);

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