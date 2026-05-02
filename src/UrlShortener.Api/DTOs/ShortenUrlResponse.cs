namespace UrlShortener.Api.DTOs;

public class ShortenUrlResponse
{
    public string ShortCode { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
}
