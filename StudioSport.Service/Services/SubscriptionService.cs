using AutoMapper;
using Microsoft.Extensions.Logging;
using StudioSport.Core.DTOs.Subscriptions;
using StudioSport.Core.Entities;
using StudioSport.Core.Enums;
using StudioSport.Core.Interfaces;
using StudioSport.Service.Exceptions;

namespace StudioSport.Service.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IMapper _mapper;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionRepository subscriptions,
        IMapper mapper,
        ILogger<SubscriptionService> logger)
    {
        _subscriptions = subscriptions;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<bool> HasActiveSubscriptionAsync(int userId, DateTime onDate, CancellationToken ct)
    {
        var sub = await _subscriptions.GetActiveByUserIdAsync(userId, onDate, ct);
        return sub is not null;
    }

    public async Task<SubscriptionResponseDto> RenewAsync(int userId, int months, CancellationToken ct)
    {
        if (months < 1 || months > 24)
            throw new ConflictException("מספר החודשים לחידוש חייב להיות בין 1 ל-24");

        var latest = await _subscriptions.GetLatestByUserIdAsync(userId, ct);
        var now = DateTime.UtcNow;

        DateTime startDate;

        // אם המנוי הקודם עדיין בתוקף - מאריכים אותו
        if (latest is not null && latest.EndDate >= now)
        {
            startDate = latest.EndDate;
            latest.Status = SubscriptionStatus.Expired;
            await _subscriptions.UpdateAsync(latest, ct);
        }
        // אם פג - פותחים חדש מהיום
        else
        {
            startDate = now;
        }

        var subscription = new Subscription
        {
            UserId = userId,
            StartDate = startDate,
            EndDate = startDate.AddMonths(months),
            Status = SubscriptionStatus.Active,
            CreatedAtUtc = now
        };

        await _subscriptions.AddAsync(subscription, ct);

        _logger.LogInformation(
            "Subscription renewed for user {UserId} until {EndDate}",
            userId, subscription.EndDate);

        return _mapper.Map<SubscriptionResponseDto>(subscription);
    }
}