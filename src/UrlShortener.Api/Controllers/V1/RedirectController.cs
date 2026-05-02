using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using UAParser;

using UrlShortener.Core.Models;
using UrlShortener.Core.Repositories;

namespace UrlShortener.Api.Controllers.V1;

[ApiController]
[ApiVersionNeutral]
public class RedirectController : ControllerBase
{
    private readonly ILinkRepository _linkRepository;
    private readonly IClickRepository _clickRepository;
    private readonly ILogger<RedirectController> _logger;

    public RedirectController(
        ILinkRepository linkRepository,
        IClickRepository clickRepository,
        ILogger<RedirectController> logger)
    {
        _linkRepository = linkRepository;
        _clickRepository = clickRepository;
        _logger = logger;
    }

    [HttpGet("/{shortCode}")]
    public async Task<IActionResult> RedirectToUrl(string shortCode, CancellationToken cancellationToken)
    {
        var link = await _linkRepository.GetByShortCodeAsync(shortCode, cancellationToken);

        if (link == null)
        {
            _logger.LogWarning("Short code not found: {ShortCode}", shortCode);
            return NotFound();
        }

        _logger.LogInformation("Redirecting {ShortCode} to {OriginalUrl}", shortCode, link.OriginalUrl);

        var userAgentString = Request.Headers.UserAgent.ToString();
        var referrer = Request.Headers.Referer.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        _ = Task.Run(async () =>
        {
            try
            {
                var device = ParseDevice(userAgentString);
                var click = new Click
                {
                    ShortCode = shortCode,
                    ClickedAt = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    Country = "Unknown",
                    UserAgent = userAgentString,
                    Referrer = referrer,
                    Device = device,
                };
                await _clickRepository.LogClickAsync(click);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log click for {ShortCode}", shortCode);
            }
        });

        return Redirect(link.OriginalUrl);
    }

    private static string ParseDevice(string userAgentString)
    {
        if (string.IsNullOrEmpty(userAgentString))
            return "Unknown";

        var parser = Parser.GetDefault();
        var clientInfo = parser.Parse(userAgentString);

        return clientInfo.Device.Family.ToLowerInvariant() switch
        {
            "spider" => "Bot",
            "other" => "Desktop",
            _ when IsTablet(clientInfo.Device.Family) => "Tablet",
            _ when IsMobile(clientInfo.UA.Family) => "Mobile",
            _ => "Desktop"
        };
    }

    private static bool IsTablet(string deviceFamily) =>
        deviceFamily.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
        deviceFamily.Contains("Tablet", StringComparison.OrdinalIgnoreCase);

    private static bool IsMobile(string uaFamily) =>
        uaFamily.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
        uaFamily.Contains("Android", StringComparison.OrdinalIgnoreCase);
}
