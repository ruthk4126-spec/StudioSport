using StudioSport.Core.Entities;

namespace StudioSport.Core.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct);

    Task<AppUser?> GetByIdAsync(int id, CancellationToken ct);

    Task AddAsync(AppUser user, CancellationToken ct);
}