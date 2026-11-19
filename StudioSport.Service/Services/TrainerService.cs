using AutoMapper;
using StudioSport.Core.DTOs.Trainers;
using StudioSport.Core.Interfaces;

namespace StudioSport.Service.Services;

public class TrainerService : ITrainerService
{
    private readonly ITrainerRepository _trainers;
    private readonly IMapper _mapper;

    public TrainerService(ITrainerRepository trainers, IMapper mapper)
    {
        _trainers = trainers;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<TrainerResponseDto>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _trainers.GetAllAsync(ct);
        return entities.Select(_mapper.Map<TrainerResponseDto>).ToList();
    }
}
