namespace UrlShortener.Api.DTOs;

public class ShortenUrlRequest
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string? CustomCode { get; set; }
}
