using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StudioSport.Core.Common;
using StudioSport.Core.Entities;
using StudioSport.Core.Enums;
using StudioSport.Core.Interfaces;
using StudioSport.Service.Services;
using Xunit;

namespace StudioSport.Tests.Services;

public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<IClassRepository> _classes = new();
    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly Mock<IFeedbackRepository> _feedbacks = new();
    private readonly Mock<ISubscriptionService> _subscriptionService = new();

    // מחזיר EnrollmentService עם ה-mocks; settings אופציונלי
    private EnrollmentService CreateSut(BookingSettings? settings = null)
    {
        return new EnrollmentService(
            _enrollments.Object,
            _classes.Object,
            _subscriptions.Object,
            _feedbacks.Object,
            NullLogger<EnrollmentService>.Instance,
            Options.Create(settings ?? new BookingSettings()),
            _subscriptionService.Object);
    }

    // שיעור עתידי תקין, שמתחיל בעוד 5 ימים
    private static ClassSession FutureSession(int id = 1, int capacity = 10) => new()
    {
        Id = id,
        Name = "אימון כוח - בוקר",
        StartTime = DateTime.UtcNow.AddDays(5),
        Capacity = capacity,
        EnrolledCount = 0,
        IsCancelled = false
    };

    private static Subscription ActiveSubscription(int total = 10, int used = 0) => new()
    {
        Id = 1,
        UserId = 2,
        TotalClasses = total,
        UsedClasses = used,
        Status = SubscriptionStatus.Active,
        StartDate = DateTime.UtcNow.AddDays(-10),
        EndDate = DateTime.UtcNow.AddDays(30)
    };

    // ---------- המקרה התקין ----------

    [Fact]
    public async Task EnrollAsync_ValidRequest_ReturnsSuccessAndConsumesCredit()
    {
        // Arrange
        var session = FutureSession();
        _classes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
        _subscriptions.Setup(r => r.GetActiveByUserIdAsync(2, session.StartTime, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(ActiveSubscription());
        _enrollments.Setup(r => r.GetByUserAndClassAsync(2, 1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment?)null);
        _classes.Setup(r => r.TryReserveSpotAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var result = await sut.EnrollAsync(1, 2, CancellationToken.None);

        // Assert
        Assert.Equal(EnrollmentOutcome.Success, result.Outcome);
        _classes.Verify(r => r.TryReserveSpotAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _enrollments.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Once);
        _subscriptionService.Verify(s => s.ConsumeCreditAsync(2, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- המקרה הנדחה: השיעור מלא ----------

    [Fact]
    public async Task EnrollAsync_ClassFull_ReturnsClassFullAndDoesNotCreateEnrollment()
    {
        // Arrange — כל הבדיקות עוברות, ורק תפיסת המקום נכשלת
        var session = FutureSession();
        _classes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
        _subscriptions.Setup(r => r.GetActiveByUserIdAsync(2, session.StartTime, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(ActiveSubscription());
        _enrollments.Setup(r => r.GetByUserAndClassAsync(2, 1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment?)null);
        _classes.Setup(r => r.TryReserveSpotAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);   // ← המקום נתפס בין הקריאה לשמירה

        var sut = CreateSut();

        // Act
        var result = await sut.EnrollAsync(1, 2, CancellationToken.None);

        // Assert
        Assert.Equal(EnrollmentOutcome.ClassFull, result.Outcome);
        Assert.Null(result.EnrollmentId);
        _enrollments.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
        _subscriptionService.Verify(s => s.ConsumeCreditAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- מקרים נוספים ----------

    [Fact]
    public async Task EnrollAsync_ClassNotFound_ReturnsClassNotFound()
    {
        _classes.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClassSession?)null);

        var result = await CreateSut().EnrollAsync(99, 2, CancellationToken.None);

        Assert.Equal(EnrollmentOutcome.ClassNotFound, result.Outcome);
    }

    [Fact]
    public async Task EnrollAsync_CancelledClass_ReturnsClassCancelled()
    {
        var session = FutureSession();
        session.IsCancelled = true;
        _classes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

        var result = await CreateSut().EnrollAsync(1, 2, CancellationToken.None);

        Assert.Equal(EnrollmentOutcome.ClassCancelled, result.Outcome);
    }

    [Fact]
    public async Task EnrollAsync_NoSubscription_ReturnsSubscriptionRequired()
    {
        var session = FutureSession();
        _classes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
        _subscriptions.Setup(r => r.GetActiveByUserIdAsync(2, session.StartTime, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Subscription?)null);

        var result = await CreateSut().EnrollAsync(1, 2, CancellationToken.None);

        Assert.Equal(EnrollmentOutcome.SubscriptionRequired, result.Outcome);
        _classes.Verify(r => r.TryReserveSpotAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_NoCreditLeft_ReturnsSubscriptionRequired()
    {
        var session = FutureSession();
        _classes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
        _subscriptions.Setup(r => r.GetActiveByUserIdAsync(2, session.StartTime, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(ActiveSubscription(total: 10, used: 10));

        var result = await CreateSut().EnrollAsync(1, 2, CancellationToken.None);

        Assert.Equal(EnrollmentOutcome.SubscriptionRequired, result.Outcome);
        _classes.Verify(r => r.TryReserveSpotAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_AlreadyEnrolled_ReturnsAlreadyEnrolled()
    {
        var session = FutureSession();
        _classes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
        _subscriptions.Setup(r => r.GetActiveByUserIdAsync(2, session.StartTime, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(ActiveSubscription());
        _enrollments.Setup(r => r.GetByUserAndClassAsync(2, 1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Enrollment { Id = 7, UserId = 2, ClassSessionId = 1 });

        var result = await CreateSut().EnrollAsync(1, 2, CancellationToken.None);

        Assert.Equal(EnrollmentOutcome.AlreadyEnrolled, result.Outcome);
        _classes.Verify(r => r.TryReserveSpotAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_DailyLimitReached_ReturnsDailyLimitReached()
    {
        var session = FutureSession();
        _classes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
        _subscriptions.Setup(r => r.GetActiveByUserIdAsync(2, session.StartTime, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(ActiveSubscription());
        _enrollments.Setup(r => r.GetByUserAndClassAsync(2, 1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment?)null);
        _enrollments.Setup(r => r.CountUserEnrollmentsOnDateAsync(2, session.StartTime, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(2);   // המכסה היא 2 כברירת מחדל

        var result = await CreateSut().EnrollAsync(1, 2, CancellationToken.None);

        Assert.Equal(EnrollmentOutcome.DailyLimitReached, result.Outcome);
    }

    [Fact]
    public async Task EnrollAsync_CancelledClassTime_ReturnsClassAlreadyStarted()
    {
        var session = FutureSession();
        session.StartTime = DateTime.UtcNow.AddHours(-1);
        _classes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

        var result = await CreateSut().EnrollAsync(1, 2, CancellationToken.None);

        Assert.Equal(EnrollmentOutcome.ClassAlreadyStarted, result.Outcome);
    }

    // ---------- הרשמה מחדש אחרי ביטול ----------

    [Fact]
    public async Task EnrollAsync_AfterCancellation_ReactivatesPreviousEnrollmentInsteadOfCreatingNew()
    {
        // Arrange — יש רשומה מבוטלת לאותו שיעור, ולכן אסור ליצור חדשה:
        // האינדקס הייחודי (ClassSessionId, UserId) היה נכשל ב-INSERT.
        var session = FutureSession();
        var cancelled = new Enrollment
        {
            Id = 42,
            UserId = 2,
            ClassSessionId = 1,
            Status = EnrollmentStatus.Cancelled,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
            CancelledAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        _classes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
        _subscriptions.Setup(r => r.GetActiveByUserIdAsync(2, session.StartTime, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(ActiveSubscription());
        _enrollments.Setup(r => r.GetByUserAndClassAsync(2, 1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment?)null);   // אין רשומה פעילה
        _enrollments.Setup(r => r.GetLatestByUserAndClassAsync(2, 1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(cancelled);           // אבל יש מבוטלת
        _classes.Setup(r => r.TryReserveSpotAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var result = await sut.EnrollAsync(1, 2, CancellationToken.None);

        // Assert
        Assert.Equal(EnrollmentOutcome.Success, result.Outcome);
        Assert.Equal(42, result.EnrollmentId);
        Assert.Equal(EnrollmentStatus.Active, cancelled.Status);
        Assert.Null(cancelled.CancelledAtUtc);

        _enrollments.Verify(r => r.UpdateAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Once);
        _enrollments.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- ביטול ----------

    [Fact]
    public async Task CancelAsync_OwnerCancels_ReleasesSpotAndRefundsCredit()
    {
        var enrollment = new Enrollment
        {
            Id = 5,
            UserId = 2,
            ClassSessionId = 1,
            Status = EnrollmentStatus.Active
        };
        _enrollments.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(enrollment);

        var result = await CreateSut().CancelAsync(5, 2, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(EnrollmentStatus.Cancelled, enrollment.Status);
        _classes.Verify(r => r.ReleaseSpotAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _subscriptionService.Verify(s => s.RefundCreditAsync(2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_NotOwner_ReturnsFalseAndDoesNotRelease()
    {
        var enrollment = new Enrollment
        {
            Id = 5,
            UserId = 2,
            ClassSessionId = 1,
            Status = EnrollmentStatus.Active
        };
        _enrollments.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(enrollment);

        var result = await CreateSut().CancelAsync(5, 99, CancellationToken.None);

        Assert.False(result);
        _classes.Verify(r => r.ReleaseSpotAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}