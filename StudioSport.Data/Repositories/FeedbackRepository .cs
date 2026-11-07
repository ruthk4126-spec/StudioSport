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
}