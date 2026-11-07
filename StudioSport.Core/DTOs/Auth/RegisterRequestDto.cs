using System.ComponentModel.DataAnnotations;

namespace StudioSport.Core.DTOs.Auth;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "שם משתמש הוא שדה חובה")]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "סיסמה היא שדה חובה")]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}