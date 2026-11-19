using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StudioSport.Core.Enums;
using StudioSport.Core.Interfaces;

namespace StudioSport.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptions;

    public SubscriptionsController(ISubscriptionService subscriptions)
        => _subscriptions = subscriptions;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("plans")]
    [AllowAnonymous]
    public IActionResult GetPlans() =>
        Ok(Enum.GetValues<SubscriptionPlan>()
                .Select(p => new { plan = p.ToString(), totalClasses = (int)p }));

    [HttpPost("request")]
    [Authorize]
    public async Task<IActionResult> RequestSubscription([FromQuery] SubscriptionPlan plan, CancellationToken ct)
    {
        try
        {
            var result = await _subscriptions.RequestAsync(CurrentUserId, plan, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> My(CancellationToken ct)
    {
        var sub = await _subscriptions.GetMyActiveAsync(CurrentUserId, ct);
        return sub is null ? NoContent() : Ok(sub);
    }

    [HttpGet("my-pending")]
    [Authorize]
    public async Task<IActionResult> MyPending(CancellationToken ct)
    {
        var mine = await _subscriptions.GetMyPendingAsync(CurrentUserId, ct);
        return mine is null ? NoContent() : Ok(mine);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Pending(CancellationToken ct)
        => Ok(await _subscriptions.GetPendingAsync(ct));

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        try
        {
            var result = await _subscriptions.ApproveAsync(id, ct);
            if (result is null) return NotFound(new { message = "המנוי לא נמצא" });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
