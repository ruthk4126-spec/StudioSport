using Microsoft.EntityFrameworkCore;
using StudioSport.Core.Entities;
using StudioSport.Core.Enums;
using StudioSport.Core.Interfaces;

namespace StudioSport.Data.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _db;

    public SubscriptionRepository(AppDbContext db) => _db = db;

    public async Task<Subscription?> GetActiveByUserIdAsync(int userId, DateTime onDate, CancellationToken ct)
        => await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId
                     && s.StartDate <= onDate
                     && s.EndDate >= onDate
                     && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(ct);

    public async Task<Subscription?> GetLatestByUserIdAsync(int userId, CancellationToken ct)
        => await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Subscription entity, CancellationToken ct)
    {
        await _db.Subscriptions.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Subscription entity, CancellationToken ct)
        => await _db.SaveChangesAsync(ct);
}