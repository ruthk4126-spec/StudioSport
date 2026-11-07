using StudioSport.Core.Entities;

namespace StudioSport.Core.Interfaces;

public interface ITrainerRepository
{
    Task<List<Trainer>> GetAllAsync(CancellationToken ct);

    Task<Trainer?> GetByIdAsync(int id, CancellationToken ct);

    Task AddAsync(Trainer entity, CancellationToken ct);
}