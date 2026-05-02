using Asp.Versioning;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using UrlShortener.Api.DTOs;
using UrlShortener.Core.Models;
using UrlShortener.Core.Repositories;
using UrlShortener.Core.Services;

namespace UrlShortener.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public class LinksController : ControllerBase
{
    private readonly ILinkRepository _linkRepository;
    private readonly ShortCodeGenerator _codeGenerator;
    private readonly IValidator<ShortenUrlRequest> _validator;
    private readonly ILogger<LinksController> _logger;

    public LinksController(
        ILinkRepository linkRepository,
        ShortCodeGenerator codeGenerator,
        IValidator<ShortenUrlRequest> validator,
        ILogger<LinksController> logger)
    {
        _linkRepository = linkRepository;
        _codeGenerator = codeGenerator;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost("shorten")]
    public async Task<ActionResult<ShortenUrlResponse>> ShortenUrl(
        [FromBody] ShortenUrlRequest request,
        CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        // Determine short code
        string shortCode;
        if (!string.IsNullOrWhiteSpace(request.CustomCode))
        {
            shortCode = request.CustomCode;

            // Check if custom code already exists
            if (await _linkRepository.ShortCodeExistsAsync(shortCode, cancellationToken))
            {
                return Conflict(new { error = "Custom code already exists" });
            }
        }
        else
        {
            // Generate unique short code
            shortCode = await GenerateUniqueShortCodeAsync(cancellationToken);
        }

        // Create link
        var link = new Link
        {
            ShortCode = shortCode,
            OriginalUrl = request.OriginalUrl,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _linkRepository.CreateAsync(link, cancellationToken);

        _logger.LogInformation("Created short URL: {ShortCode} -> {OriginalUrl}", shortCode, request.OriginalUrl);

        // Build response
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var response = new ShortenUrlResponse
        {
            ShortCode = shortCode,
            ShortUrl = $"{baseUrl}/{shortCode}",
            OriginalUrl = request.OriginalUrl
        };

        return Ok(response);
    }

    [HttpDelete("links/{shortCode}")]
    public async Task<IActionResult> DeleteLink(string shortCode, CancellationToken cancellationToken)
    {
        var deleted = await _linkRepository.SoftDeleteAsync(shortCode, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning("Delete requested for unknown short code: {ShortCode}", shortCode);
            return NotFound();
        }

        _logger.LogInformation("Soft deleted short URL: {ShortCode}", shortCode);
        return NoContent();
    }

    private async Task<string> GenerateUniqueShortCodeAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            var code = _codeGenerator.Generate();
            if (!await _linkRepository.ShortCodeExistsAsync(code, cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Failed to generate unique short code after multiple attempts");
    }
}
