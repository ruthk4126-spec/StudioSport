namespace StudioSport.Core.Dtos;

public class ClassDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int Capacity { get; set; }
    public int EnrolledCount { get; set; }
    public string? EquipmentNotes { get; set; }
    public bool IsCancelled { get; set; }
    public int TrainerId { get; set; }
}
