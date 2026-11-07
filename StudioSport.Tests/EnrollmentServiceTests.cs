using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StudioSport.Core.Common;
using StudioSport.Core.Entities;
using StudioSport.Core.Enums;
using StudioSport.Core.Interfaces;
using StudioSport.Service.Services;
using Xunit;

namespace StudioSport.Tests;

public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<IClassRepository> _classes = new();
    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly Mock<IFeedbackRepository> _feedbacks = new();
    private readonly Mock<ILogger<EnrollmentService>> _logger = new();

    private EnrollmentService CreateSut(string? connectionString = null)
        => new(
            _enrollments.Object,
            _classes.Object,
            _subscriptions.Object,
            _feedbacks.Object,
            _logger.Object,
            Options.Create(new BookingSettings
            {
                MinDaysBeforeClass = 7,
                MaxClassesPerDay = 2
            }));

    private static ClassSession BuildClass(int capacity = 5, int enrolled = 0, int daysAhead = 10)
        => new()
        {
            Id = 1,
            Name = "אימון כוח",
            StartTime = DateTime.UtcNow.AddDays(daysAhead),
            Capacity = capacity,
            EnrolledCount = enrolled,
            TrainerId = 1
        };

    // --- המקרה התקין ---
    [Fact]
    public async Task EnrollAsync_WhenSpotAvailable_ReturnsSuccess()
    {
        // Arrange
        _classes.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildClass());

        _subscriptions.Setup(x => x.GetActiveByUserIdAsync(7, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new Subscription { Id = 1, UserId = 7 });

        _enrollments.Setup(x => x.GetByUserAndClassAsync(7, 1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment?)null);

        _enrollments.Setup(x => x.CountUserEnrollmentsOnDateAsync(7, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(0);

        // כאן הלב: תפיסת המקום מצליחה
        _classes.Setup(x => x.TryReserveSpotAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        _enrollments.Setup(x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
                    .Callback<Enrollment, CancellationToken>((e, _) => e.Id = 55)
                    .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.EnrollAsync(1, 7, CancellationToken.None);

        // Assert
        Assert.Equal(EnrollmentOutcome.Success, result.Outcome);
        Assert.Equal(55, result.EnrollmentId);
        _classes.Verify(x => x.TryReserveSpotAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- המקרה הנדחה: השיעור התמלא ---
    [Fact]
    public async Task EnrollAsync_WhenClassFull_ReturnsClassFull()
    {
        // Arrange
        _classes.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildClass());

        _subscriptions.Setup(x => x.GetActiveByUserIdAsync(7, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new Subscription { Id = 1, UserId = 7 });

        _enrollments.Setup(x => x.GetByUserAndClassAsync(7, 1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment?)null);

        _enrollments.Setup(x => x.CountUserEnrollmentsOnDateAsync(7, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(0);

        // תפיסת המקום נכשלה - מישהו אחר הקדים
        _classes.Setup(x => x.TryReserveSpotAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        var sut = CreateSut();

        // Act
        var result = await sut.EnrollAsync(1, 7, CancellationToken.None);

        // Assert
        Assert.Equal(EnrollmentOutcome.ClassFull, result.Outcome);
        Assert.Null(result.EnrollmentId);

        // לא נוצר רישום
        _enrollments.Verify(x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- חלון הרישום סגור ---
    [Fact]
    public async Task EnrollAsync_WhenWithinSevenDays_ReturnsBookingWindowClosed()
    {
        _classes.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildClass(daysAhead: 3));

        var sut = CreateSut();

        var result = await sut.EnrollAsync(1, 7, CancellationToken.None);

        Assert.Equal(EnrollmentOutcome.BookingWindowClosed, result.Outcome);
        _classes.Verify(x => x.TryReserveSpotAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- אין מנוי ---
    [Fact]
    public async Task EnrollAsync_WithoutSubscription_ReturnsSubscriptionRequired()
    {
        _classes.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildClass());

        _subscriptions.Setup(x => x.GetActiveByUserIdAsync(7, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Subscription?)null);

        var sut = CreateSut();

        var result = await sut.EnrollAsync(1, 7, CancellationToken.None);

        Assert.Equal(EnrollmentOutcome.SubscriptionRequired, result.Outcome);
    }

    // --- השיעור לא נמצא ---
    [Fact]
    public async Task EnrollAsync_WhenClassMissing_ReturnsClassNotFound()
    {
        _classes.Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClassSession?)null);

        var sut = CreateSut();

        var result = await sut.EnrollAsync(99, 7, CancellationToken.None);

        Assert.Equal(EnrollmentOutcome.ClassNotFound, result.Outcome);
    }
}