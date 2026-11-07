using StudioSport.Core.Entities;

namespace StudioSport.Core.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetActiveByUserIdAsync(int userId, DateTime onDate, CancellationToken ct);

    Task<Subscription?> GetLatestByUserIdAsync(int userId, CancellationToken ct);

    Task AddAsync(Subscription entity, CancellationToken ct);

    Task UpdateAsync(Subscription entity, CancellationToken ct);
}
