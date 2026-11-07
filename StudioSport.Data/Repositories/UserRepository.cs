using Microsoft.EntityFrameworkCore;
using StudioSport.Core.Entities;
using StudioSport.Core.Interfaces;

namespace StudioSport.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct)
        => await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<AppUser?> GetByIdAsync(int id, CancellationToken ct)
        => await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct)
    {
        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
    }
}