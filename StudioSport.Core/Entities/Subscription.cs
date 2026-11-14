using StudioSport.Core.Enums;

namespace StudioSport.Core.Entities;

public class Subscription
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public SubscriptionPlan Plan { get; set; }      // Single / Four / Ten / Twenty

               // מתעדכן בהרשמה וביטול
    public int TotalClasses { get; set; } = 10;
    public int UsedClasses { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
}