using StudioSport.Core.Entities;

namespace StudioSport.Core.Interfaces;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(int id, CancellationToken ct);

    Task<Enrollment?> GetByUserAndClassAsync(int userId, int classId, CancellationToken ct);

    /// מחזיר את הרשומה האחרונה של המשתמשת בשיעור, בכל סטטוס.
    /// נדרש כדי להחיות רשומה שבוטלה, במקום ליצור כפילות שמפרה את האינדקס הייחודי.
    Task<Enrollment?> GetLatestByUserAndClassAsync(int userId, int classId, CancellationToken ct);

    Task<List<Enrollment>> GetUserEnrollmentsAsync(int userId, CancellationToken ct);

    Task<int> CountUserEnrollmentsOnDateAsync(int userId, DateTime date, CancellationToken ct);
    
    Task AddAsync(Enrollment entity, CancellationToken ct);

    Task UpdateAsync(Enrollment entity, CancellationToken ct);

    /// מבטל את כל הרישומים הפעילים של שיעור, ומחזיר את מספרם.
    Task<int> CancelActiveByClassAsync(int classId, DateTime cancelledAtUtc, CancellationToken ct);
}