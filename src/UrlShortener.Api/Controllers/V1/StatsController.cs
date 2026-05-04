using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using UrlShortener.Api.DTOs;
using UrlShortener.Core.Models;
using UrlShortener.Core.Repositories;

namespace UrlShortener.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public class StatsController : ControllerBase
{
    private readonly IClickRepository _clickRepository;
    private readonly ILinkRepository _linkRepository;
    private readonly ILogger<StatsController> _logger;

    public StatsController(
        IClickRepository clickRepository,
        ILinkRepository linkRepository,
        ILogger<StatsController> logger)
    {
        _clickRepository = clickRepository;
        _linkRepository = linkRepository;
        _logger = logger;
    }

    [HttpGet("stats/{shortCode}")]
    public async Task<ActionResult<StatsResponse>> GetStats(string shortCode, CancellationToken cancellationToken)
    {
        var link = await _linkRepository.GetByShortCodeAsync(shortCode, cancellationToken);
        if (link == null)
        {
            _logger.LogWarning("Stats requested for unknown short code: {ShortCode}", shortCode);
            return NotFound();
        }

        var stats = await _clickRepository.GetStatsAsync(shortCode, cancellationToken);

        var response = new StatsResponse
        {
            TotalClicks = stats.TotalClicks,
            ClicksByDay = stats.ClicksByDay.Select(d => new DailyClickDto(d.Date, d.Count)).ToList(),
            TopReferrers = stats.TopReferrers.Select(r => new ReferrerCountDto(r.Referrer, r.Count)).ToList(),
            CountryBreakdown = stats.CountryBreakdown.Select(c => new CountryCountDto(c.Country, c.Count)).ToList(),
            DeviceBreakdown = stats.DeviceBreakdown.Select(d => new DeviceCountDto(d.Device, d.Count)).ToList(),
        };

        return Ok(response);
    }
    
    [HttpGet("stats/all")]
    public async Task<ActionResult<IEnumerable<StatsResponse>>> GetStats(CancellationToken cancellationToken)
    {
        var shortCodes = await _linkRepository.GetAllShortCodesAsync(cancellationToken);
        IEnumerable<string> enumerable = shortCodes.ToList();
        if (!enumerable.Any())
        {
            _logger.LogWarning("No record found.");
            return NotFound();
        }

        var stats = await _clickRepository.GetAllClicksAsync(enumerable, cancellationToken);

        List<StatsResponse> responses = new();
        foreach (ClickStats stat in stats)
        {
            var response = new StatsResponse
            {
                TotalClicks = stat.TotalClicks,
                ClicksByDay = stat.ClicksByDay.Select(d => new DailyClickDto(d.Date, d.Count)).ToList(),
                TopReferrers = stat.TopReferrers.Select(r => new ReferrerCountDto(r.Referrer, r.Count)).ToList(),
                CountryBreakdown = stat.CountryBreakdown.Select(c => new CountryCountDto(c.Country, c.Count)).ToList(),
                DeviceBreakdown = stat.DeviceBreakdown.Select(d => new DeviceCountDto(d.Device, d.Count)).ToList(),
            };
            responses.Add(response);
        }

        return Ok(responses);
    }
}
