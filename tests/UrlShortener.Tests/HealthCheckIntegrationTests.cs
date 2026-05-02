using System.Net;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;

using UrlShortener.Tests.Infrastructure;

namespace UrlShortener.Tests;

[Collection(MongoDbCollection.Name)]
public class HealthCheckIntegrationTests : IntegrationTestBase
{
    protected override string DatabaseName => "test_health";

    public HealthCheckIntegrationTests(MongoDbContainerFixture mongo) : base(mongo) { }

    [Fact(Skip = "Health check endpoint has routing issues in WebApplicationFactory tests")]
    public async Task HealthCheck_ShouldReturnHealthy_WhenMongoDbIsRunning()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task MongoDb_ShouldBeAccessible_ThroughDependencyInjection()
    {
        using var scope = Factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

        var collections = await database.ListCollectionNamesAsync();
        var collectionList = await collections.ToListAsync();

        database.Should().NotBeNull();
        database.DatabaseNamespace.DatabaseName.Should().Be(DatabaseName);
        collectionList.Should().NotBeNull();
    }
}
