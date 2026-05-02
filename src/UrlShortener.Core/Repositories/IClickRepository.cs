using UrlShortener.Core.Models;

namespace UrlShortener.Core.Repositories;

public interface IClickRepository
{
    Task LogClickAsync(Click click, CancellationToken cancellationToken = default);
    Task<ClickStats> GetStatsAsync(string shortCode, CancellationToken cancellationToken = default);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}
