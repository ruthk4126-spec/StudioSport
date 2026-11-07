using StudioSport.Core.DTOs.Subscriptions;

namespace StudioSport.Core.Interfaces;

public interface ISubscriptionService
{
    Task<bool> HasActiveSubscriptionAsync(int userId, DateTime onDate, CancellationToken ct);
    Task<SubscriptionResponseDto> RenewAsync(int userId, int months, CancellationToken ct);
  
}