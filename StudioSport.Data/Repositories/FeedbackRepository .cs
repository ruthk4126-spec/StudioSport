using Microsoft.EntityFrameworkCore;
using StudioSport.Core.Entities;
using StudioSport.Core.Interfaces;

namespace StudioSport.Data.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly AppDbContext _db;

    public FeedbackRepository(AppDbContext db) => _db = db;

    public async Task<Feedback?> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct)
        => await _db.Feedbacks
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.EnrollmentId == enrollmentId, ct);

    public async Task AddAsync(Feedback entity, CancellationToken ct)
    {
        await _db.Feedbacks.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<HashSet<int>> GetEnrollmentIdsWithFeedbackAsync(
        IEnumerable<int> enrollmentIds, CancellationToken ct)
    {
        var ids = enrollmentIds.ToList();
        if (ids.Count == 0)
            return new HashSet<int>();

        var withFeedback = await _db.Feedbacks
            .AsNoTracking()
            .Where(f => ids.Contains(f.EnrollmentId))
            .Select(f => f.EnrollmentId)
            .ToListAsync(ct);

        return withFeedback.ToHashSet();
    }
}