using UrlShortener.Core.Models;

namespace UrlShortener.Core.Repositories;

public interface ILinkRepository
{
    Task<Link?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
    Task<Link?> GetByOriginalUrlAsync(string originalUrl, string? campaign, CancellationToken cancellationToken = default);
    Task<Link> CreateAsync(Link link, CancellationToken cancellationToken = default);
    Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(string shortCode, CancellationToken cancellationToken = default);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Link>> GetAllLinksAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllShortCodesAsync(CancellationToken cancellationToken = default);
}
