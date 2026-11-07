namespace StudioSport.Core.Common;

public enum EnrollmentOutcome
{
    Success = 0,
    ClassFull = 1,
    AlreadyEnrolled = 2,
    ClassNotFound = 3,
    ClassAlreadyStarted = 4,
    ClassCancelled = 5,
    BookingWindowClosed = 6,
    SubscriptionRequired = 7,
    DailyLimitReached = 8,
    FeedbackNotAllowed = 9
}

public class EnrollmentResult
{
    public EnrollmentOutcome Outcome { get; set; }

    public int? EnrollmentId { get; set; }

    public static EnrollmentResult Success(int enrollmentId) =>
        new() { Outcome = EnrollmentOutcome.Success, EnrollmentId = enrollmentId };

    public static EnrollmentResult Failure(EnrollmentOutcome outcome) =>
        new() { Outcome = outcome };
}