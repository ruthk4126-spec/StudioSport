using StudioSport.Core.Common;
using StudioSport.Core.DTOs.Classes;
using StudioSport.Core.Entities;

namespace StudioSport.Core.Interfaces;

public interface IClassRepository
{
    Task<ClassSession?> GetByIdAsync(int id, CancellationToken ct);
    Task<ClassSession?> GetForUpdateAsync(int id, CancellationToken ct);
    Task<PagedResult<ClassSession>> GetPagedAsync(ClassQueryParameters query, CancellationToken ct);
    Task AddAsync(ClassSession entity, CancellationToken ct);
    Task UpdateAsync(ClassSession entity, CancellationToken ct);
    Task DeleteAsync(ClassSession entity, CancellationToken ct);

    /// תפיסת מקום אטומית. מחזיר false אם השיעור מלא או מבוטל.
    Task<bool> TryReserveSpotAsync(int classId, CancellationToken ct);

    Task ReleaseSpotAsync(int classId, CancellationToken ct);
}