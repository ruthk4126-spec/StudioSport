namespace StudioSport.Core.DTOs.Classes;

public class ClassQueryParameters
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
    }

    public DateTime? FromDate { get; set; }

    public int? TrainerId { get; set; }

    // לדוגמה: "startTime" למיון עולה, "-startTime" ליורד
    public string? SortBy { get; set; }
}