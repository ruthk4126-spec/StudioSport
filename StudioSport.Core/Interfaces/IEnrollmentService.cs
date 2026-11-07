using StudioSport.Core.Common;
using StudioSport.Core.DTOs.Enrollments;

namespace StudioSport.Core.Interfaces;

public interface IEnrollmentService
{
    Task<EnrollmentResult> EnrollAsync(int classId, int userId, CancellationToken ct);

    Task<bool> CancelAsync(int enrollmentId, int userId, CancellationToken ct);

    Task<List<EnrollmentResponseDto>> GetMyEnrollmentsAsync(int userId, CancellationToken ct);

    Task<EnrollmentResult> AddFeedbackAsync(int enrollmentId, int userId, FeedbackCreateDto dto, CancellationToken ct);
}