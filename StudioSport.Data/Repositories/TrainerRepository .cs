using Microsoft.EntityFrameworkCore;
using StudioSport.Core.Entities;
using StudioSport.Core.Interfaces;

namespace StudioSport.Data.Repositories;

public class TrainerRepository : ITrainerRepository
{
    private readonly AppDbContext _db;

    public TrainerRepository(AppDbContext db) => _db = db;

    public async Task<List<Trainer>> GetAllAsync(CancellationToken ct)
        => await _db.Trainers
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<Trainer?> GetByIdAsync(int id, CancellationToken ct)
        => await _db.Trainers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(Trainer entity, CancellationToken ct)
    {
        await _db.Trainers.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }
}