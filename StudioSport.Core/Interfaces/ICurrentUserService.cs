using StudioSport.Core.Enums;

namespace StudioSport.Core.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }

    UserRole? Role { get; }

    bool IsAdmin { get; }
}