using StudioSport.Core.Enums;

namespace StudioSport.Core.Entities;

public class Enrollment
{
    public int Id { get; set; }

    // many-to-many בפועל: משתמשת <-> שיעור
    public int ClassSessionId { get; set; }
    public ClassSession? ClassSession { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledAtUtc { get; set; }
}