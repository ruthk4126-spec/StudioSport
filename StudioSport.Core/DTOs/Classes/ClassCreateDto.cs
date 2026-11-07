using System.ComponentModel.DataAnnotations;

namespace StudioSport.Core.DTOs.Classes;

public class ClassCreateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; }

    [Range(1, 60, ErrorMessage = "הקיבולת חייבת להיות בין 1 ל-60")]
    public int Capacity { get; set; }

    [Required]
    public int TrainerId { get; set; }

    [StringLength(500)]
    public string? EquipmentNotes { get; set; }
}