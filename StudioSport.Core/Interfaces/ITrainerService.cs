using StudioSport.Core.DTOs.Trainers;

namespace StudioSport.Core.Interfaces;

public interface ITrainerService
{
    Task<IReadOnlyList<TrainerResponseDto>> GetAllAsync(CancellationToken ct);
}
