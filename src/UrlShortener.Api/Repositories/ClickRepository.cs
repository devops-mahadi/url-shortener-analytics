using MongoDB.Bson;
using MongoDB.Driver;

using UrlShortener.Core.Models;
using UrlShortener.Core.Repositories;

namespace UrlShortener.Api.Repositories;

public class ClickRepository : IClickRepository
{
    private readonly IMongoCollection<Click> _clicks;

    public ClickRepository(IMongoDatabase database)
    {
        _clicks = database.GetCollection<Click>("clicks");
    }

    public async Task LogClickAsync(Click click, CancellationToken cancellationToken = default)
    {
        await _clicks.InsertOneAsync(click, cancellationToken: cancellationToken);
    }

    public async Task<ClickStats> GetStatsAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        var totalClicks = await _clicks
            .CountDocumentsAsync(c => c.ShortCode == shortCode, cancellationToken: cancellationToken);

        var clicksByDay = await _clicks.Aggregate()
            .Match(c => c.ShortCode == shortCode && c.ClickedAt >= cutoff)
            .Group(c => c.ClickedAt.ToString("yyyy-MM-dd"),
                g => new { Date = g.Key, Count = g.Count() })
            .SortByDescending(g => g.Date)
            .ToListAsync(cancellationToken);

        var topReferrers = await _clicks.Aggregate()
            .Match(c => c.ShortCode == shortCode)
            .Group(c => c.Referrer, g => new { Referrer = g.Key, Count = g.Count() })
            .SortByDescending(g => g.Count)
            .Limit(5)
            .ToListAsync(cancellationToken);

        var countryBreakdown = await _clicks.Aggregate()
            .Match(c => c.ShortCode == shortCode)
            .Group(c => c.Country, g => new { Country = g.Key, Count = g.Count() })
            .SortByDescending(g => g.Count)
            .Limit(10)
            .ToListAsync(cancellationToken);

        var deviceBreakdown = await _clicks.Aggregate()
            .Match(c => c.ShortCode == shortCode)
            .Group(c => c.Device, g => new { Device = g.Key, Count = g.Count() })
            .SortByDescending(g => g.Count)
            .ToListAsync(cancellationToken);

        return new ClickStats
        {
            TotalClicks = totalClicks,
            ClicksByDay = clicksByDay.Select(x => new DailyClick(x.Date, x.Count)).ToList(),
            TopReferrers = topReferrers.Select(x => new ReferrerCount(x.Referrer, x.Count)).ToList(),
            CountryBreakdown = countryBreakdown.Select(x => new CountryCount(x.Country, x.Count)).ToList(),
            DeviceBreakdown = deviceBreakdown.Select(x => new DeviceCount(x.Device, x.Count)).ToList(),
        };
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var compoundIndex = Builders<Click>.IndexKeys
            .Ascending(c => c.ShortCode)
            .Descending(c => c.ClickedAt);

        await _clicks.Indexes.CreateOneAsync(
            new CreateIndexModel<Click>(compoundIndex),
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ClickStats>> GetAllClicksAsync(IEnumerable<string> shortCodes, CancellationToken cancellationToken = default)
    {
        List<ClickStats> clickStatsList = new();
        foreach (string shortCode in shortCodes)
        {
            clickStatsList.Add(await GetStatsAsync(shortCode,  cancellationToken));
        }
        return clickStatsList;
    }
}
