using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

using UrlShortener.Api.DTOs;
using UrlShortener.Tests.Infrastructure;

namespace UrlShortener.Tests;

[Collection(MongoDbCollection.Name)]
public class RedirectIntegrationTests : IntegrationTestBase
{
    protected override string DatabaseName => "test_redirect";

    protected override WebApplicationFactoryClientOptions ClientOptions => new()
    {
        AllowAutoRedirect = false,
    };

    public RedirectIntegrationTests(MongoDbContainerFixture mongo) : base(mongo) { }

    [Fact]
    public async Task Redirect_WithValidShortCode_ShouldRedirectToOriginalUrl()
    {
        var shortenRequest = new ShortenUrlRequest { OriginalUrl = "https://www.example.com" };
        var shortenResponse = await Client.PostAsJsonAsync("/api/v1/shorten", shortenRequest);
        var shortenResult = await shortenResponse.Content.ReadFromJsonAsync<ShortenUrlResponse>();

        var redirectResponse = await Client.GetAsync($"/{shortenResult!.ShortCode}");

        redirectResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        redirectResponse.Headers.Location.Should().NotBeNull();
        redirectResponse.Headers.Location!.ToString().Should().StartWith("https://www.example.com");
    }

    [Fact]
    public async Task Redirect_WithNonExistentShortCode_ShouldReturnNotFound()
    {
        var response = await Client.GetAsync("/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Redirect_WithCustomCode_ShouldRedirectToOriginalUrl()
    {
        var shortenRequest = new ShortenUrlRequest
        {
            OriginalUrl = "https://www.github.com",
            CustomCode = "github",
        };
        await Client.PostAsJsonAsync("/api/v1/shorten", shortenRequest);

        var redirectResponse = await Client.GetAsync("/github");

        redirectResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        redirectResponse.Headers.Location!.ToString().Should().StartWith("https://www.github.com");
    }
}
