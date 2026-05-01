using MongoDB.Driver;

using Serilog;

using UrlShortener.Core.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting UrlShortener API");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

            var mongoSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>()
                ?? throw new Exception("MongoDB settings are not configured properly.");
            builder.Services.AddSingleton(mongoSettings);

            // Register MongoDB client
            builder.Services.AddSingleton<IMongoClient>(sp =>
            {
                var settings = sp.GetRequiredService<MongoDbSettings>();
                return new MongoClient(settings.ConnectionString);
            });

            // Register MongoDB database
            builder.Services.AddSingleton<IMongoDatabase>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                var settings = sp.GetRequiredService<MongoDbSettings>();
                return client.GetDatabase(settings.DatabaseName);
            });

            builder.Services.AddHealthChecks()
                .AddMongoDb(
                    sp => sp.GetRequiredService<IMongoClient>(),
                    name: "MongoDB",
                    timeout: TimeSpan.FromSeconds(5),
                    tags: new[] { "db", "mongo" }
                );
            
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            if(app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapHealthChecks("/health");
            app.MapControllers();
            Log.Information("UrlShortener API started successfully");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}