using Microsoft.EntityFrameworkCore;
using StudioSport.Core.Common;
using StudioSport.Core.DTOs.Classes;
using StudioSport.Core.Entities;
using StudioSport.Core.Interfaces;

namespace StudioSport.Data.Repositories;

public class ClassRepository : IClassRepository
{
    private readonly AppDbContext _db;

    public ClassRepository(AppDbContext db) => _db = db;

    public async Task<ClassSession?> GetByIdAsync(int id, CancellationToken ct)
        => await _db.ClassSessions
            .AsNoTracking()
            .Include(c => c.Trainer)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<ClassSession?> GetForUpdateAsync(int id, CancellationToken ct)
        => await _db.ClassSessions
            .Include(c => c.Trainer)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PagedResult<ClassSession>> GetPagedAsync(ClassQueryParameters q, CancellationToken ct)
    {
        IQueryable<ClassSession> query = _db.ClassSessions
            .AsNoTracking()
            .Include(c => c.Trainer);

        if (q.FromDate.HasValue)
            query = query.Where(c => c.StartTime >= q.FromDate.Value);

        if (q.TrainerId.HasValue)
            query = query.Where(c => c.TrainerId == q.TrainerId.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.StartTime)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ClassSession>
        {
            Items = items,
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = total
        };
    }

    public async Task AddAsync(ClassSession entity, CancellationToken ct)
    {
        _db.ClassSessions.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ClassSession entity, CancellationToken ct)
        => await _db.SaveChangesAsync(ct);

    public async Task DeleteAsync(ClassSession entity, CancellationToken ct)
    {
        _db.ClassSessions.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> TryReserveSpotAsync(int classId, CancellationToken ct)
    {
        var session = await _db.ClassSessions
            .FirstOrDefaultAsync(c => c.Id == classId, ct);

        if (session is null || session.IsCancelled || session.EnrolledCount >= session.Capacity)
            return false;

        session.EnrolledCount++;
        session.Version = Guid.NewGuid();

        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task ReleaseSpotAsync(int classId, CancellationToken ct)
    {
        await _db.ClassSessions
            .Where(c => c.Id == classId && c.EnrolledCount > 0)
            .ExecuteUpdateAsync(
                s => s.SetProperty(c => c.EnrolledCount, c => c.EnrolledCount - 1),
                ct);
    }
}