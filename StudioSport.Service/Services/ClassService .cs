using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioSport.Core.Common;
using StudioSport.Core.DTOs.Classes;
using StudioSport.Core.Entities;
using StudioSport.Core.Enums;
using StudioSport.Core.Interfaces;
using StudioSport.Data;
using StudioSport.Service.Exceptions;

namespace StudioSport.Service.Services;

public class ClassService : IClassService
{
    private readonly IClassRepository _classes;
    private readonly ITrainerRepository _trainers;
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<ClassService> _logger;

    public ClassService(
        IClassRepository classes,
        ITrainerRepository trainers,
        AppDbContext db,
        IMapper mapper,
        ILogger<ClassService> logger)
    {
        _classes = classes;
        _trainers = trainers;
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }
    public async Task<ClassStatsDto> GetStatsAsync(int days, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var until = now.AddDays(days);

        var q = _db.ClassSessions
            .AsNoTracking()
            .Where(c => !c.IsCancelled && c.StartTime > now && c.StartTime <= until);

        var total = await q.CountAsync(ct);
        var free = await q.SumAsync(c => c.Capacity - c.EnrolledCount, ct);

        return new ClassStatsDto { Total = total, Free = free };
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
        entity.StartTime = DateTime.SpecifyKind(entity.StartTime, DateTimeKind.Utc);
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

        if (entity.IsCancelled)
            return false;

        var enrollments = await _db.Enrollments
            .Where(e => e.ClassSessionId == id && e.Status == EnrollmentStatus.Active)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var e in enrollments)
        {
            e.Status = EnrollmentStatus.Cancelled;
            e.CancelledAtUtc = now;
        }

        entity.IsCancelled = true;
        entity.EnrolledCount = 0;

        await _classes.UpdateAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Class {ClassId} cancelled; {Count} enrollments cancelled", id, enrollments.Count);
        return true;
    }
}