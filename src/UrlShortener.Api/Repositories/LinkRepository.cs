using MongoDB.Driver;

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

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var indexKeys = Builders<Link>.IndexKeys.Ascending(l => l.ShortCode);
        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<Link>(indexKeys, indexOptions);

        await _links.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }
}
