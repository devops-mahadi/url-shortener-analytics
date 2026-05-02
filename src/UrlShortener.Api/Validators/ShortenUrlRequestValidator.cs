using FluentValidation;

using UrlShortener.Api.DTOs;
using UrlShortener.Core.Services;

namespace UrlShortener.Api.Validators;

public class ShortenUrlRequestValidator : AbstractValidator<ShortenUrlRequest>
{
    public ShortenUrlRequestValidator(ShortCodeGenerator codeGenerator)
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty()
            .WithMessage("URL is required")
            .Must(BeAValidUrl)
            .WithMessage("Invalid URL format");

        RuleFor(x => x.CustomCode)
            .Must(code => code == null || codeGenerator.IsValidFormat(code))
            .WithMessage("Custom code must be 3-20 characters and contain only alphanumeric characters");
    }

    private bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
