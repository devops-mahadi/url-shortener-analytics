using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using UrlShortener.Api.DTOs;
using UrlShortener.Tests.Infrastructure;

namespace UrlShortener.Tests;

[Collection(MongoDbCollection.Name)]
public class ShortenUrlIntegrationTests : IntegrationTestBase
{
    protected override string DatabaseName => "test_shorten";

    public ShortenUrlIntegrationTests(MongoDbContainerFixture mongo) : base(mongo) { }

    [Fact]
    public async Task ShortenUrl_WithValidUrl_ShouldReturnShortCode()
    {
        var request = new ShortenUrlRequest { OriginalUrl = "https://www.example.com" };

        var response = await Client.PostAsJsonAsync("/api/v1/shorten", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShortenUrlResponse>();
        result.Should().NotBeNull();
        result!.ShortCode.Should().NotBeNullOrEmpty();
        result.ShortCode.Length.Should().Be(7);
        result.OriginalUrl.Should().Be("https://www.example.com");
        result.ShortUrl.Should().Contain(result.ShortCode);
    }

    [Fact]
    public async Task ShortenUrl_WithCustomCode_ShouldUseCustomCode()
    {
        var request = new ShortenUrlRequest
        {
            OriginalUrl = "https://www.example.com",
            CustomCode = "custom123",
        };

        var response = await Client.PostAsJsonAsync("/api/v1/shorten", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShortenUrlResponse>();
        result.Should().NotBeNull();
        result!.ShortCode.Should().Be("custom123");
    }

    [Fact]
    public async Task ShortenUrl_WithDuplicateCustomCode_ShouldReturnConflict()
    {
        var request = new ShortenUrlRequest
        {
            OriginalUrl = "https://www.example.com",
            CustomCode = "duplicate",
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/v1/shorten", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await Client.PostAsJsonAsync("/api/v1/shorten", request);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShortenUrl_WithInvalidUrl_ShouldReturnBadRequest()
    {
        var request = new ShortenUrlRequest { OriginalUrl = "not-a-valid-url" };

        var response = await Client.PostAsJsonAsync("/api/v1/shorten", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShortenUrl_WithEmptyUrl_ShouldReturnBadRequest()
    {
        var request = new ShortenUrlRequest { OriginalUrl = "" };

        var response = await Client.PostAsJsonAsync("/api/v1/shorten", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
