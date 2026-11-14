namespace StudioSport.Core.DTOs.Enrollments;

public class MyEnrollmentsDto
{
    public List<EnrollmentResponseDto> Active { get; set; } = new();
    public List<EnrollmentResponseDto> History { get; set; } = new();
}