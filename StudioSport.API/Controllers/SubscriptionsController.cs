using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StudioSport.Core.DTOs.Subscriptions;
using StudioSport.Core.Enums;
using StudioSport.Core.Interfaces;

namespace StudioSport.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptions;
    private readonly ISubscriptionRepository _repository;
    private readonly IMapper _mapper;

    public SubscriptionsController(
        ISubscriptionService subscriptions,
        ISubscriptionRepository repository,
        IMapper mapper)
    {
        _subscriptions = subscriptions;
        _repository = repository;
        _mapper = mapper;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("plans")]
    [AllowAnonymous]
    public IActionResult GetPlans() =>
        Ok(Enum.GetValues<SubscriptionPlan>()
                .Select(p => new { plan = p.ToString(), totalClasses = (int)p }));

    [HttpPost("request")]
    [Authorize]
    public async Task<IActionResult> Request([FromQuery] SubscriptionPlan plan, CancellationToken ct)
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
        var sub = await _repository.GetActiveByUserIdAsync(CurrentUserId, DateTime.UtcNow, ct);
        if (sub is null) return NoContent();
        return Ok(_mapper.Map<SubscriptionResponseDto>(sub));
    }

    [HttpGet("my-pending")]
    [Authorize]
    public async Task<IActionResult> MyPending(CancellationToken ct)
    {
        var all = await _subscriptions.GetPendingAsync(ct);
        var mine = all.FirstOrDefault(s => s.UserId == CurrentUserId);
        return mine is null ? NoContent() : Ok(mine);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Pending(CancellationToken ct)
    {
        var pending = await _subscriptions.GetPendingAsync(ct);
        return Ok(pending);
    }

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


