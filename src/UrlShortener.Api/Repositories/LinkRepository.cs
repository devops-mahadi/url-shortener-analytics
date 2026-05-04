using MongoDB.Driver;
using MongoDB.Driver.Linq;

using UrlShortener.Core.Models;
using UrlShortener.Core.Repositories;

namespace UrlShortener.Api.Repositories;

public class LinkRepository : ILinkRepository
{
    private readonly IMongoCollection<Link> _links;

    public LinkRepository(IMongoDatabase database)
    {
        _links = database.GetCollection<Link>("links");
    }

    public async Task<Link?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        return await _links
            .Find(l => l.ShortCode == shortCode && !l.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Link?> GetByOriginalUrlAsync(string originalUrl, string? campaign, CancellationToken cancellationToken = default)
    {
        return await _links
            .Find(l => l.OriginalUrl == originalUrl && l.Campaign == campaign && !l.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Link> CreateAsync(Link link, CancellationToken cancellationToken = default)
    {
        await _links.InsertOneAsync(link, cancellationToken: cancellationToken);
        return link;
    }

    public async Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        return await _links
            .Find(l => l.ShortCode == shortCode)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var update = Builders<Link>.Update.Set(l => l.IsDeleted, true);
        var result = await _links.UpdateOneAsync(
            l => l.ShortCode == shortCode && !l.IsDeleted,
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var shortCodeIndex = new CreateIndexModel<Link>(
            Builders<Link>.IndexKeys.Ascending(l => l.ShortCode),
            new CreateIndexOptions { Unique = true });

        var dedupIndex = new CreateIndexModel<Link>(
            Builders<Link>.IndexKeys.Ascending(l => l.OriginalUrl).Ascending(l => l.Campaign));

        await _links.Indexes.CreateManyAsync([shortCodeIndex, dedupIndex], cancellationToken);
    }

    public async Task<IEnumerable<Link>> GetAllLinksAsync(CancellationToken cancellationToken = default)
    {
        return await _links.AsQueryable().ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAllShortCodesAsync(CancellationToken cancellationToken = default)
    {
        return await _links.AsQueryable().Select(l => l.ShortCode).ToListAsync(cancellationToken);
    }
}
