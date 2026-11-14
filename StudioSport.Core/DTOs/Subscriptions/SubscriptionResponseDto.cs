namespace StudioSport.Core.DTOs.Subscriptions;

public class SubscriptionResponseDto
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;


    public string Plan { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public int TotalClasses { get; set; }

    public int UsedClasses { get; set; }

    // מחושב - מספר השיעורים שנותרו
    public int RemainingClasses => TotalClasses - UsedClasses;

    // מחושב - האם המנוי תקף עכשיו
    public bool IsActive { get; set; }
}