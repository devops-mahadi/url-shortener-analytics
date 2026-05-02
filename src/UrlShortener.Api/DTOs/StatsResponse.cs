namespace UrlShortener.Api.DTOs;

public class StatsResponse
{
    public long TotalClicks { get; set; }
    public List<DailyClickDto> ClicksByDay { get; set; } = [];
    public List<ReferrerCountDto> TopReferrers { get; set; } = [];
    public List<CountryCountDto> CountryBreakdown { get; set; } = [];
    public List<DeviceCountDto> DeviceBreakdown { get; set; } = [];
}

public record DailyClickDto(string Date, long Count);
public record ReferrerCountDto(string Referrer, long Count);
public record CountryCountDto(string Country, long Count);
public record DeviceCountDto(string Device, long Count);
