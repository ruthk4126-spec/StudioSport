using StudioSport.Core.DTOs.Subscriptions;
using StudioSport.Core.Enums;

namespace StudioSport.Core.Interfaces;

public interface ISubscriptionService
{
    Task<bool> HasActiveSubscriptionAsync(int userId, DateTime onDate, CancellationToken ct);
    Task<SubscriptionResponseDto> RenewAsync(int userId, int months, CancellationToken ct);
    Task<SubscriptionResponseDto> RequestAsync(int userId, SubscriptionPlan plan, CancellationToken ct);
    Task<bool> HasAvailableCreditAsync(int userId, CancellationToken ct);
    Task<IReadOnlyList<SubscriptionResponseDto>> GetPendingAsync(CancellationToken ct);
    Task<SubscriptionResponseDto?> ApproveAsync(int subscriptionId, CancellationToken ct);
    Task ConsumeCreditAsync(int userId, CancellationToken ct);
    Task RefundCreditAsync(int userId, CancellationToken ct);
}