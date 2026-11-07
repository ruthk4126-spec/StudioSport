namespace StudioSport.Core.Entities;

public class Trainer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Specialty { get; set; }

    // one-to-many: מדריכה אחת, שיעורים רבים
    public ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();
}