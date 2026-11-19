using StudioSport.Core.Entities;

namespace StudioSport.Core.Interfaces;

public interface IFeedbackRepository
{
    Task<Feedback?> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct);

    Task AddAsync(Feedback entity, CancellationToken ct);

    /// מחזיר את מזהי הרישומים שיש להם כבר משוב, מתוך רשימה נתונה.
    /// שאילתה אחת במקום בדיקה לכל רשומה (N+1).
    Task<HashSet<int>> GetEnrollmentIdsWithFeedbackAsync(IEnumerable<int> enrollmentIds, CancellationToken ct);
}