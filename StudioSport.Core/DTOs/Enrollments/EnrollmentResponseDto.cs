namespace StudioSport.Core.DTOs.Enrollments;

public class EnrollmentResponseDto
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    // מה להביא לשיעור - מוצג אחרי ההזמנה
    public string? EquipmentNotes { get; set; }
}