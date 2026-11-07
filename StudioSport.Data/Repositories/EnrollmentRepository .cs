using Microsoft.EntityFrameworkCore;
using StudioSport.Core.Entities;
using StudioSport.Core.Enums;
using StudioSport.Core.Interfaces;

namespace StudioSport.Data.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly AppDbContext _db;

    public EnrollmentRepository(AppDbContext db) => _db = db;

    public async Task<Enrollment?> GetByIdAsync(int id, CancellationToken ct)
        => await _db.Enrollments
            .Include(e => e.ClassSession)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Enrollment?> GetByUserAndClassAsync(int userId, int classId, CancellationToken ct)
        => await _db.Enrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId
                                   && e.ClassSessionId == classId
                                   && e.Status == EnrollmentStatus.Active, ct);

    public async Task<List<Enrollment>> GetUserEnrollmentsAsync(int userId, CancellationToken ct)
        => await _db.Enrollments
            .AsNoTracking()
            .Include(e => e.ClassSession)
                .ThenInclude(c => c!.Trainer)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.ClassSession!.StartTime)
            .ToListAsync(ct);

    public async Task<int> CountUserEnrollmentsOnDateAsync(int userId, DateTime date, CancellationToken ct)
    {
        var from = date.Date.ToUniversalTime();
        var to = from.AddDays(1);

        return await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.UserId == userId
                     && e.Status == EnrollmentStatus.Active
                     && e.ClassSession!.StartTime >= from
                     && e.ClassSession.StartTime < to)
            .CountAsync(ct);
    }

    public async Task AddAsync(Enrollment entity, CancellationToken ct)
    {
        await _db.Enrollments.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Enrollment entity, CancellationToken ct)
        => await _db.SaveChangesAsync(ct);
}