namespace StudioSport.Core.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public string Role { get; set; } = string.Empty;
}