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
            .AsNoTracking()
            .Where(c => !c.IsCancelled && c.StartTime > now);

        // הספירה על שאילתה נקייה מ-Include — אין טעם לטעון את ה-Trainer רק כדי לספור
        var total = await q.CountAsync(ct);

        var items = await q
            .Include(c => c.Trainer)
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
        // UPDATE אטומי אחד: התנאי "יש מקום פנוי" יושב בתוך ה-WHERE,
        // ולכן אין חלון בין הקריאה לכתיבה. שתי בקשות מקבילות לא יכולות
        // לקחת את המקום האחרון — רק הראשונה תמצא שורה לעדכן.
        // ExecuteUpdate לא עובר דרך ה-ChangeTracker, ולכן ה-concurrency token
        // אינו נבדק כאן; הוא עדיין מוגן על ידי ה-WHERE.
        var affectedRows = await _db.ClassSessions
            .Where(c => c.Id == classId
                     && !c.IsCancelled
                     && c.EnrolledCount < c.Capacity)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(c => c.EnrolledCount, c => c.EnrolledCount + 1)
                    .SetProperty(c => c.Version, _ => Guid.NewGuid()),
                ct);

        // 0 שורות = או שהשיעור לא קיים/בוטל, או שהמקום נתפס בינתיים
        return affectedRows > 0;
    }

    public async Task<(int Total, int Free)> GetStatsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var q = _db.ClassSessions
            .AsNoTracking()
            .Where(c => !c.IsCancelled && c.StartTime >= now);

        // שאילתה אחת — במקום Count ואז Sum על שתי שאילתות נפרדות
        var row = await q
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Free = g.Sum(c => c.Capacity - c.EnrolledCount)
            })
            .FirstOrDefaultAsync(ct);

        return (row?.Total ?? 0, row?.Free ?? 0);
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
