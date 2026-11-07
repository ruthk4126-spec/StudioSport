using StudioSport.Core.Common;
using StudioSport.Core.DTOs.Classes;

namespace StudioSport.Core.Interfaces;

public interface IClassService
{
    Task<PagedResult<ClassResponseDto>> GetPagedAsync(ClassQueryParameters query, CancellationToken ct);

    Task<ClassResponseDto?> GetByIdAsync(int id, CancellationToken ct);

    Task<ClassResponseDto> CreateAsync(ClassCreateDto dto, CancellationToken ct);

    Task<ClassResponseDto?> UpdateAsync(int id, ClassUpdateDto dto, CancellationToken ct);

    Task<bool> CancelClassAsync(int id, CancellationToken ct);
}