namespace UrlShortener.Core.Models;

public class ClickStats
{
    public long TotalClicks { get; set; }
    public List<DailyClick> ClicksByDay { get; set; } = [];
    public List<ReferrerCount> TopReferrers { get; set; } = [];
    public List<CountryCount> CountryBreakdown { get; set; } = [];
    public List<DeviceCount> DeviceBreakdown { get; set; } = [];
}

public record DailyClick(string Date, long Count);
public record ReferrerCount(string Referrer, long Count);
public record CountryCount(string Country, long Count);
public record DeviceCount(string Device, long Count);
