namespace StudioSport.Core.Entities;

public class ClassTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public int Capacity { get; set; }
    public string? EquipmentNotes { get; set; }
    public int TrainerId { get; set; }
    public Trainer? Trainer { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? GeneratedUntil { get; set; }
}