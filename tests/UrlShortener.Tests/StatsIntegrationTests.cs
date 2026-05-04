using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using UrlShortener.Api.DTOs;
using UrlShortener.Core.Models;
using UrlShortener.Core.Repositories;
using UrlShortener.Tests.Infrastructure;

namespace UrlShortener.Tests;

[Collection(MongoDbCollection.Name)]
public class StatsIntegrationTests : IntegrationTestBase
{
    protected override string DatabaseName => "test_stats";

    protected override WebApplicationFactoryClientOptions ClientOptions => new()
    {
        AllowAutoRedirect = false,
    };

    public StatsIntegrationTests(MongoDbContainerFixture mongo) : base(mongo) { }

    [Fact]
    public async Task GetStats_WithNonExistentShortCode_ShouldReturnNotFound()
    {
        var response = await Client.GetAsync("/api/v1/stats/doesnotexist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStats_WithNoClicks_ShouldReturnZeroTotals()
    {
        var shortenRequest = new ShortenUrlRequest { OriginalUrl = "https://www.example.com" };
        var shortenResponse = await Client.PostAsJsonAsync("/api/v1/shorten", shortenRequest);
        var shortenResult = await shortenResponse.Content.ReadFromJsonAsync<ShortenUrlResponse>();

        var statsResponse = await Client.GetAsync($"/api/v1/stats/{shortenResult!.ShortCode}");

        statsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await statsResponse.Content.ReadFromJsonAsync<StatsResponse>();
        stats.Should().NotBeNull();
        stats!.TotalClicks.Should().Be(0);
        stats.ClicksByDay.Should().BeEmpty();
        stats.TopReferrers.Should().BeEmpty();
        stats.CountryBreakdown.Should().BeEmpty();
        stats.DeviceBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStats_AfterRedirect_ShouldCountClick()
    {
        var shortenRequest = new ShortenUrlRequest
        {
            OriginalUrl = "https://www.example.com",
            CustomCode = "statstest",
        };
        await Client.PostAsJsonAsync("/api/v1/shorten", shortenRequest);

        await Client.GetAsync("/statstest");
        await Task.Delay(500);

        var statsResponse = await Client.GetAsync("/api/v1/stats/statstest");
        statsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await statsResponse.Content.ReadFromJsonAsync<StatsResponse>();
        stats.Should().NotBeNull();
        stats!.TotalClicks.Should().Be(1);
        stats.DeviceBreakdown.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetStats_WithMultipleClicks_ShouldAggregateCorrectly()
    {
        var shortenRequest = new ShortenUrlRequest
        {
            OriginalUrl = "https://www.example.com",
            CustomCode = "multiclick",
        };
        await Client.PostAsJsonAsync("/api/v1/shorten", shortenRequest);

        await Client.GetAsync("/multiclick");
        await Client.GetAsync("/multiclick");
        await Client.GetAsync("/multiclick");
        await Task.Delay(500);

        var statsResponse = await Client.GetAsync("/api/v1/stats/multiclick");
        var stats = await statsResponse.Content.ReadFromJsonAsync<StatsResponse>();
        stats!.TotalClicks.Should().Be(3);
    }

    [Fact]
    public async Task GetAllStats_WithNoLinks_ShouldReturnNotFound()
    {
        var response = await Client.GetAsync("/api/v1/stats/all");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllStats_WithMultipleLinks_ShouldReturnStatsForEach()
    {
        await Client.PostAsJsonAsync("/api/v1/shorten",
            new ShortenUrlRequest { OriginalUrl = "https://www.example.com", CustomCode = "allstats1" });
        await Client.PostAsJsonAsync("/api/v1/shorten",
            new ShortenUrlRequest { OriginalUrl = "https://www.github.com", CustomCode = "allstats2" });

        using var scope = Factory.Services.CreateScope();
        var clickRepo = scope.ServiceProvider.GetRequiredService<IClickRepository>();

        await clickRepo.LogClickAsync(new Click
        {
            ShortCode = "allstats1",
            ClickedAt = DateTime.UtcNow,
            IpAddress = "1.2.3.4",
            Country = "US",
            UserAgent = "TestAgent",
            Referrer = "https://google.com",
            Device = "Desktop",
        });

        var response = await Client.GetAsync("/api/v1/stats/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<List<StatsResponse>>();
        stats.Should().NotBeNull();
        stats!.Should().HaveCount(2);
        stats.Sum(s => s.TotalClicks).Should().Be(1);
    }

    [Fact]
    public async Task GetStats_ClicksByDay_ShouldGroupByDate()
    {
        const string code = "daystats";
        await Client.PostAsJsonAsync("/api/v1/shorten",
            new ShortenUrlRequest { OriginalUrl = "https://www.example.com", CustomCode = code });

        using var scope = Factory.Services.CreateScope();
        var clickRepo = scope.ServiceProvider.GetRequiredService<IClickRepository>();

        await clickRepo.LogClickAsync(new Click
        {
            ShortCode = code,
            ClickedAt = DateTime.UtcNow,
            IpAddress = "1.2.3.4",
            Country = "US",
            UserAgent = "TestAgent",
            Referrer = "https://google.com",
            Device = "Desktop",
        });

        await clickRepo.LogClickAsync(new Click
        {
            ShortCode = code,
            ClickedAt = DateTime.UtcNow,
            IpAddress = "1.2.3.5",
            Country = "GB",
            UserAgent = "TestAgent",
            Referrer = "https://twitter.com",
            Device = "Mobile",
        });

        var statsResponse = await Client.GetAsync($"/api/v1/stats/{code}");
        var stats = await statsResponse.Content.ReadFromJsonAsync<StatsResponse>();

        stats!.TotalClicks.Should().Be(2);
        stats.ClicksByDay.Should().HaveCount(1);
        stats.ClicksByDay[0].Count.Should().Be(2);
        stats.CountryBreakdown.Should().HaveCount(2);
        stats.DeviceBreakdown.Should().HaveCount(2);
        stats.TopReferrers.Should().HaveCount(2);
    }
}
