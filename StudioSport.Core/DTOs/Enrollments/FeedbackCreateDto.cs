using System.ComponentModel.DataAnnotations;

namespace StudioSport.Core.DTOs.Enrollments;

public class FeedbackCreateDto
{
    [Range(1, 5, ErrorMessage = "הדירוג חייב להיות בין 1 ל-5")]
    public int Rating { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }
}