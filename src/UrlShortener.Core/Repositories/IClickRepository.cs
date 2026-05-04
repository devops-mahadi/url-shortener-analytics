using UrlShortener.Core.Models;

namespace UrlShortener.Core.Repositories;

public interface IClickRepository
{
    Task LogClickAsync(Click click, CancellationToken cancellationToken = default);
    Task<ClickStats> GetStatsAsync(string shortCode, CancellationToken cancellationToken = default);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ClickStats>> GetAllClicksAsync(IEnumerable<string> shortCodes, CancellationToken cancellationToken = default);
}
