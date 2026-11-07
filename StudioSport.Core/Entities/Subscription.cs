using StudioSport.Core.Enums;

namespace StudioSport.Core.Entities;

public class Subscription
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}