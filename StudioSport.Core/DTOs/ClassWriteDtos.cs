namespace StudioSport.Core.Dtos;

public class CreateClassDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }   // UTC
    public int Capacity { get; set; }
    public string? EquipmentNotes { get; set; }
    public int TrainerId { get; set; }
}

public class UpdateClassDto
{
    public string? Name { get; set; }
    public DateTime? StartTime { get; set; }   // UTC
    public int? Capacity { get; set; }
    public string? EquipmentNotes { get; set; }
    public int? TrainerId { get; set; }
}
