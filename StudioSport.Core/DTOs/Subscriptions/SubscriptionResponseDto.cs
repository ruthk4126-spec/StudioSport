namespace StudioSport.Core.DTOs.Subscriptions;

public class SubscriptionResponseDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    // מחושב - האם המנוי תקף עכשיו
    public bool IsActive { get; set; }
}