using UrlShortener.Core.Repositories;

namespace UrlShortener.Api.Services;

public class MongoDbIndexService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MongoDbIndexService> _logger;

    public MongoDbIndexService(
        IServiceProvider serviceProvider,
        ILogger<MongoDbIndexService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating MongoDB indexes...");

        using var scope = _serviceProvider.CreateScope();
        var linkRepository = scope.ServiceProvider.GetRequiredService<ILinkRepository>();
        var clickRepository = scope.ServiceProvider.GetRequiredService<IClickRepository>();

        await linkRepository.EnsureIndexesAsync(cancellationToken);
        await clickRepository.EnsureIndexesAsync(cancellationToken);

        _logger.LogInformation("MongoDB indexes created successfully");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
