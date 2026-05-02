using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UrlShortener.Core.Models;

public class Link
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("shortCode")]
    public string ShortCode { get; set; } = string.Empty;

    [BsonElement("originalUrl")]
    public string OriginalUrl { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("createdBy")]
    public string? CreatedBy { get; set; }

    [BsonElement("campaign")]
    public string? Campaign { get; set; }

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
