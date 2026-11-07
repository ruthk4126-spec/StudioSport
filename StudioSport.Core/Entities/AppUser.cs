using StudioSport.Core.Enums;

namespace StudioSport.Core.Entities;

public class AppUser
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    // נשמרת סיסמה מוצפנת בלבד - לעולם לא הסיסמה עצמה
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Member;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // one-to-many: משתמשת אחת, רישומים רבים
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    // one-to-many: משתמשת אחת, מנויים רבים לאורך הזמן
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}