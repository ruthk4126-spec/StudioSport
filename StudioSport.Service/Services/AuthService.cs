using Microsoft.Extensions.Logging;
using StudioSport.Core.DTOs.Auth;
using StudioSport.Core.Entities;
using StudioSport.Core.Enums;
using StudioSport.Core.Interfaces;
using StudioSport.Service.Exceptions;

namespace StudioSport.Service.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        ITokenService tokens,
        ILogger<AuthService> logger)
    {
        _users = users;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct)
    {
        var existing = await _users.GetByUsernameAsync(dto.Username, ct);
        if (existing is not null)
            throw new ConflictException("שם המשתמש תפוס");

        var user = new AppUser
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Member,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _users.AddAsync(user, ct);

        var (token, expiresAt) = _tokens.CreateToken(user);

        _logger.LogInformation("User {UserId} registered", user.Id);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(dto.Username, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;

        var (token, expiresAt) = _tokens.CreateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            Role = user.Role.ToString()
        };
    }
}