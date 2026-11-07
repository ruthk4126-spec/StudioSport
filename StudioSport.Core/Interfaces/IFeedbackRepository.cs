using StudioSport.Core.Entities;

namespace StudioSport.Core.Interfaces;

public interface IFeedbackRepository
{
    Task<Feedback?> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct);

    Task AddAsync(Feedback entity, CancellationToken ct);
}