using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudioSport.Core.Common;
using StudioSport.Core.DTOs.Enrollments;
using StudioSport.Core.Interfaces;

namespace StudioSport.API.Controllers;

[ApiController]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollments;

    public EnrollmentsController(IEnrollmentService enrollments) => _enrollments = enrollments;

    /// הרישום לשיעור - כאן נולד ה-409
    [HttpPost("api/classes/{classId:int}/enrollments")]
    [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enroll([FromRoute] int classId, CancellationToken ct)
    {
        var userId = GetUserId();

        var result = await _enrollments.EnrollAsync(classId, userId, ct);

        if (result.Outcome != EnrollmentOutcome.Success)
            return MapFailure(result.Outcome);

        return Created($"/api/enrollments/{result.EnrollmentId}", new
        {
            enrollmentId = result.EnrollmentId
        });
    }

    [HttpDelete("api/enrollments/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel([FromRoute] int id, CancellationToken ct)
    {
        var userId = GetUserId();
        var ok = await _enrollments.CancelAsync(id, userId, ct);

        return ok ? NoContent() : Forbid();
    }

    [HttpGet("api/enrollments/my")]
    [ProducesResponseType(typeof(List<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => Ok(await _enrollments.GetMyEnrollmentsAsync(GetUserId(), ct));

    [HttpPost("api/enrollments/{id:int}/feedback")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddFeedback([FromRoute] int id, [FromBody] FeedbackCreateDto dto, CancellationToken ct)
    {
        var result = await _enrollments.AddFeedbackAsync(id, GetUserId(), dto, ct);

        if (result.Outcome != EnrollmentOutcome.Success)
            return MapFailure(result.Outcome);

        return StatusCode(StatusCodes.Status201Created);
    }

    private int GetUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IActionResult MapFailure(EnrollmentOutcome outcome) => outcome switch
    {
        EnrollmentOutcome.ClassFull => Conflict(new { message = "השיעור מלא" }),
        EnrollmentOutcome.AlreadyEnrolled => Conflict(new { message = "כבר רשומה לשיעור זה" }),
        EnrollmentOutcome.ClassNotFound => NotFound(new { message = "שיעור לא נמצא" }),
        EnrollmentOutcome.ClassAlreadyStarted => BadRequest(new { message = "השיעור כבר התקיים" }),
        EnrollmentOutcome.ClassCancelled => BadRequest(new { message = "השיעור בוטל" }),
        EnrollmentOutcome.BookingWindowClosed => BadRequest(new { message = "הרישום לשיעור זה נסגר" }),
        EnrollmentOutcome.SubscriptionRequired => StatusCode(403, new { message = "נדרש מנוי פעיל. ניתן לחדש" }),
        EnrollmentOutcome.DailyLimitReached => Conflict(new { message = "הגעת למכסת השיעורים היומית" }),
        EnrollmentOutcome.FeedbackNotAllowed => BadRequest(new { message = "לא ניתן לתת משוב לשיעור זה" }),
        _ => BadRequest()
    };
}