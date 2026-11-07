using Microsoft.AspNetCore.Mvc;
using StudioSport.Core.DTOs.Auth;
using StudioSport.Core.Entities;
using StudioSport.Core.Interfaces;
using StudioSport.Service.Exceptions;
using Microsoft.EntityFrameworkCore;
using StudioSport.Data;

namespace StudioSport.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;

    public AuthController(AppDbContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequestDto dto, CancellationToken ct)
    {
        var exists = await _db.Users.AnyAsync(u => u.Username == dto.Username, ct);
        if (exists)
            throw new ConflictException("שם המשתמש תפוס");

        var user = new AppUser
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = Core.Enums.UserRole.Member
        };

        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = _tokens.CreateToken(user);

        return CreatedAtAction(nameof(Register), new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            Role = user.Role.ToString()
        });
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequestDto dto, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == dto.Username, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "שם משתמש או סיסמה שגויים" });

        var (token, expiresAt) = _tokens.CreateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            Role = user.Role.ToString()
        });
    }
}