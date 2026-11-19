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

    // ===== חדש: בקשת מנוי חדשה (ממתין לאישור מנהל) =====
    public async Task<SubscriptionResponseDto> RequestAsync(int userId, SubscriptionPlan plan, CancellationToken ct)
    {
        var total = (int)plan;
        if (total <= 0)
            throw new ArgumentException("חבילת מנוי לא תקינה", nameof(plan));

        // אין לפתוח בקשה חדשה כשיש מנוי פעיל
        var active = await _subscriptions.GetActiveByUserIdAsync(userId, DateTime.UtcNow, ct);
        if (active is not null)
            throw new InvalidOperationException("קיים מנוי פעיל, לא ניתן לפתוח בקשה חדשה.");
        var pending = await _subscriptions.GetPendingAsync(ct);
        if (pending.Any(s => s.UserId == userId))
            throw new InvalidOperationException("קיימת בקשה ממתינה לאישור.");
        

        var now = DateTime.UtcNow;

        var subscription = new Subscription
        {
            UserId = userId,
            Plan = plan,
            TotalClasses = total,
            UsedClasses = 0,
            Status = SubscriptionStatus.Pending,
            StartDate = now,            // ייקבע מחדש בעת האישור
            EndDate = now.AddYears(1),  // ייקבע מחדש בעת האישור
            CreatedAtUtc = now
        };

        await _subscriptions.AddAsync(subscription, ct);

        _logger.LogInformation(
            "Subscription requested by user {UserId}: plan {Plan} ({Total} classes), status Pending",
            userId, plan, total);

        return _mapper.Map<SubscriptionResponseDto>(subscription);
    }
    public async Task<IReadOnlyList<SubscriptionResponseDto>> GetPendingAsync(CancellationToken ct)
    {
        var pending = await _subscriptions.GetPendingAsync(ct);

        return pending.Select(s =>
        {
            var dto = _mapper.Map<SubscriptionResponseDto>(s);
            dto.Username = s.User?.Username ?? string.Empty;
            return dto;
        }).ToList();
    }

    public async Task<SubscriptionResponseDto?> GetMyActiveAsync(int userId, CancellationToken ct)
    {
        var sub = await _subscriptions.GetActiveByUserIdAsync(userId, DateTime.UtcNow, ct);
        if (sub is null) return null;

        var dto = _mapper.Map<SubscriptionResponseDto>(sub);
        dto.Username = sub.User?.Username ?? string.Empty;
        return dto;
    }

    public async Task<SubscriptionResponseDto?> GetMyPendingAsync(int userId, CancellationToken ct)
    {
        var pending = await _subscriptions.GetPendingAsync(ct);
        var mine = pending.FirstOrDefault(s => s.UserId == userId);
        if (mine is null) return null;

        var dto = _mapper.Map<SubscriptionResponseDto>(mine);
        dto.Username = mine.User?.Username ?? string.Empty;
        return dto;
    }

    public async Task<SubscriptionResponseDto?> ApproveAsync(int subscriptionId, CancellationToken ct)
    {
        var sub = await _subscriptions.GetByIdAsync(subscriptionId, ct);
        if (sub is null) return null;

        if (sub.Status != SubscriptionStatus.Pending)
            throw new InvalidOperationException("המנוי אינו ממתין לאישור");

        var now = DateTime.UtcNow;
        sub.Status = SubscriptionStatus.Active;
        sub.StartDate = now;
        sub.EndDate = now.AddYears(1);
        sub.ApprovedAtUtc = now;

        await _subscriptions.UpdateAsync(sub, ct);

        _logger.LogInformation(
            "Subscription {Id} approved for user {UserId}: plan {Plan} ({Total} classes)",
            sub.Id, sub.UserId, sub.Plan, sub.TotalClasses);

        var dto = _mapper.Map<SubscriptionResponseDto>(sub);
        dto.Username = sub.User?.Username ?? string.Empty;
        return dto;
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
        else
        {
            startDate = now;
        }

        // שומרים על החבילה של המנוי הקודם (וברירת מחדל: עשרה שיעורים)
        var plan = latest?.Plan ?? SubscriptionPlan.Ten;

        var subscription = new Subscription
        {
            UserId = userId,
            Plan = plan,
            StartDate = startDate,
            EndDate = startDate.AddMonths(months),
            Status = SubscriptionStatus.Active,
            CreatedAtUtc = now,
            TotalClasses = (int)plan,
            UsedClasses = 0,
            ApprovedAtUtc = now
        };

        await _subscriptions.AddAsync(subscription, ct);

        _logger.LogInformation(
            "Subscription renewed for user {UserId} until {EndDate}",
            userId, subscription.EndDate);

        return _mapper.Map<SubscriptionResponseDto>(subscription);
    }

    public async Task<bool> HasAvailableCreditAsync(int userId, CancellationToken ct)
    {
        var sub = await _subscriptions.GetActiveByUserIdAsync(userId, DateTime.UtcNow, ct);
        return sub is not null && sub.TotalClasses > sub.UsedClasses;
    }

    public async Task ConsumeCreditAsync(int userId, CancellationToken ct)
    {
        var sub = await _subscriptions.GetActiveByUserIdAsync(userId, DateTime.UtcNow, ct)
                  ?? throw new ConflictException("אין מנוי פעיל.");

        if (sub.TotalClasses <= sub.UsedClasses)
            throw new ConflictException("אין יתרת שיעורים. יש לחדש את המנוי.");

        sub.UsedClasses++;
        await _subscriptions.UpdateAsync(sub, ct);
    }

    public async Task RefundCreditAsync(int userId, CancellationToken ct)
    {
        var sub = await _subscriptions.GetActiveByUserIdAsync(userId, DateTime.UtcNow, ct);
        if (sub is null || sub.UsedClasses <= 0) return;

        sub.UsedClasses--;
        await _subscriptions.UpdateAsync(sub, ct);

        _logger.LogInformation(
            "Credit refunded for user {UserId}: {Used}/{Total}",
            userId, sub.UsedClasses, sub.TotalClasses);
    }
}