using Microsoft.AspNetCore.Mvc;
using StudioSport.Core.DTOs.Auth;
using StudioSport.Core.Interfaces;

namespace StudioSport.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequestDto dto, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequestDto dto, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(dto, ct);

        if (result is null)
            return Unauthorized(new { message = "שם משתמש או סיסמה שגויים" });

        return Ok(result);
    }
}