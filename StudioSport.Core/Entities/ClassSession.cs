namespace StudioSport.Core.Entities;

public class ClassSession
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    // המשאב המוגבל
    public int Capacity { get; set; }

    // המונה המתעדכן אטומית - לא נגזר מ-COUNT
    public int EnrolledCount { get; set; }

    // מה להביא לשיעור - מוצג למשתמשת אחרי ההזמנה
    public string? EquipmentNotes { get; set; }

    public bool IsCancelled { get; set; }

    // many-to-one: שיעור שייך למדריכה
    public int TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    // one-to-many: שיעור אחד, רישומים רבים
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public Guid Version { get; set; } = Guid.NewGuid();

}