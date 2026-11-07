using StudioSport.Core.Entities;

namespace StudioSport.Core.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(AppUser user);
}