namespace StudioSport.Core.DTOs.Classes;

public class ClassResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public string TrainerName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int EnrolledCount { get; set; }

    // מחושב בשרת, לא מגיע מהלקוח
    public int AvailableSpots => Capacity - EnrolledCount;

    public string? EquipmentNotes { get; set; }

    public bool IsCancelled { get; set; }
}