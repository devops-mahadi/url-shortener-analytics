using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MongoDB.Driver;

using UrlShortener.Core.Configuration;

namespace UrlShortener.Tests.Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly MongoDbContainerFixture _mongo;
    private WebApplicationFactory<Program> _factory = null!;

    protected HttpClient Client { get; private set; } = null!;
    protected WebApplicationFactory<Program> Factory => _factory;

    // Each subclass gets its own database — no cross-test pollution
    protected abstract string DatabaseName { get; }

    // Override to configure the HttpClient (e.g. disable auto-redirect)
    protected virtual WebApplicationFactoryClientOptions ClientOptions => new();

    protected IntegrationTestBase(MongoDbContainerFixture mongo)
    {
        _mongo = mongo;
    }

    public virtual Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<MongoDbSettings>();
                    services.RemoveAll<IMongoClient>();
                    services.RemoveAll<IMongoDatabase>();

                    var mongoSettings = new MongoDbSettings
                    {
                        ConnectionString = _mongo.ConnectionString,
                        DatabaseName = DatabaseName,
                    };

                    services.AddSingleton(mongoSettings);

                    services.AddSingleton<IMongoClient>(_ => new MongoClient(_mongo.ConnectionString));

                    services.AddSingleton<IMongoDatabase>(sp =>
                        sp.GetRequiredService<IMongoClient>().GetDatabase(DatabaseName));
                });
            });

        Client = _factory.CreateClient(ClientOptions);
        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
