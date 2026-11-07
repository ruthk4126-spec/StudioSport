namespace StudioSport.Core.Common;

public class BookingSettings
{
    public int MinDaysBeforeClass { get; set; } = 7;

    public int MaxClassesPerDay { get; set; } = 2;
}