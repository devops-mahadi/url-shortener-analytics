using Asp.Versioning;

using FluentValidation;

using MongoDB.Driver;

using Serilog;

using UrlShortener.Api.Repositories;
using UrlShortener.Api.Services;
using UrlShortener.Core.Configuration;
using UrlShortener.Core.Repositories;
using UrlShortener.Core.Services;

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

            // Register application services
            builder.Services.AddSingleton<ShortCodeGenerator>();
            builder.Services.AddScoped<ILinkRepository, LinkRepository>();
            builder.Services.AddScoped<IClickRepository, ClickRepository>();

            // Register hosted services
            builder.Services.AddHostedService<MongoDbIndexService>();

            // Register FluentValidation
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            // Add API Versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            if(app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            // Map endpoints
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