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

    public async Task<PagedResult<ClassSession>> GetPagedAsync(ClassQueryParameters query, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var q = _db.ClassSessions
            .Include(c => c.Trainer)
            .Where(c => !c.IsCancelled && c.StartTime > now);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(c => c.StartTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ClassSession>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
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