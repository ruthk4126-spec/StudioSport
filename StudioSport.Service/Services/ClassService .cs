using AutoMapper;
using Microsoft.Extensions.Logging;
using StudioSport.Core.Common;
using StudioSport.Core.DTOs.Classes;
using StudioSport.Core.Entities;
using StudioSport.Core.Interfaces;
using StudioSport.Service.Exceptions;

namespace StudioSport.Service.Services;

public class ClassService : IClassService
{
    private readonly IClassRepository _classes;
    private readonly ITrainerRepository _trainers;
    private readonly IMapper _mapper;
    private readonly ILogger<ClassService> _logger;

    public ClassService(
        IClassRepository classes,
        ITrainerRepository trainers,
        IMapper mapper,
        ILogger<ClassService> logger)
    {
        _classes = classes;
        _trainers = trainers;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<ClassResponseDto>> GetPagedAsync(ClassQueryParameters query, CancellationToken ct)
    {
        var paged = await _classes.GetPagedAsync(query, ct);

        return new PagedResult<ClassResponseDto>
        {
            Items = paged.Items.Select(_mapper.Map<ClassResponseDto>).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }

    public async Task<ClassResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _classes.GetByIdAsync(id, ct);
        return entity is null ? null : _mapper.Map<ClassResponseDto>(entity);
    }

    public async Task<ClassResponseDto> CreateAsync(ClassCreateDto dto, CancellationToken ct)
    {
        if (dto.StartTime <= DateTime.UtcNow)
            throw new ConflictException("לא ניתן לקבוע שיעור במועד שעבר");

        var trainer = await _trainers.GetByIdAsync(dto.TrainerId, ct);
        if (trainer is null)
            throw new NotFoundException($"מדריכה {dto.TrainerId} לא נמצאה");

        var entity = _mapper.Map<ClassSession>(dto);
        entity.EnrolledCount = 0;
        entity.IsCancelled = false;

        await _classes.AddAsync(entity, ct);

        _logger.LogInformation("Class {ClassId} created", entity.Id);

        var created = await _classes.GetByIdAsync(entity.Id, ct);
        return _mapper.Map<ClassResponseDto>(created!);
    }

    public async Task<ClassResponseDto?> UpdateAsync(int id, ClassUpdateDto dto, CancellationToken ct)
    {
        var entity = await _classes.GetForUpdateAsync(id, ct);
        if (entity is null)
            return null;

        if (entity.IsCancelled)
            throw new ConflictException("לא ניתן לעדכן שיעור שבוטל");

        // אי אפשר להקטין קיבולת מתחת לכמות הנרשמות בפועל
        if (dto.Capacity < entity.EnrolledCount)
            throw new ConflictException(
                $"לא ניתן להקטין את הקיבולת מתחת ל-{entity.EnrolledCount} הנרשמות");

        _mapper.Map(dto, entity);
        await _classes.UpdateAsync(entity, ct);

        _logger.LogInformation("Class {ClassId} updated", id);

        var updated = await _classes.GetByIdAsync(id, ct);
        return _mapper.Map<ClassResponseDto>(updated!);
    }

    public async Task<bool> CancelClassAsync(int id, CancellationToken ct)
    {
        var entity = await _classes.GetForUpdateAsync(id, ct);
        if (entity is null)
            return false;

        entity.IsCancelled = true;
        await _classes.UpdateAsync(entity, ct);

        _logger.LogWarning("Class {ClassId} cancelled", id);

        return true;
    }
}