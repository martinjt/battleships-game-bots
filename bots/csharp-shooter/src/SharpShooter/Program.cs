using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SharpShooter;
using SharpShooter.Tournament;
using SharpShooter.Tournament.Models;

// Configure OpenTelemetry
var botName = Environment.GetEnvironmentVariable("BOT_NAME") ?? "LinqToVictory";
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? "http://otel-collector.battleships.svc.cluster.local:4317";

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(serviceName: botName, serviceVersion: "1.0.0"))
    .AddSource(botName)
    .AddHttpClientInstrumentation()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri(otlpEndpoint);
        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    })
    .Build();

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

// Check if tournament mode is enabled
var tournamentMode = Environment.GetEnvironmentVariable("TOURNAMENT_MODE")?.ToLowerInvariant() == "true";

// Setup cancellation for graceful shutdown
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\nShutting down gracefully...");
};

try
{
    if (tournamentMode)
    {
        var logger = loggerFactory.CreateLogger<TournamentClient>();
        var config = TournamentConfig.FromEnvironment();

        logger.LogInformation("Starting in TOURNAMENT MODE");
        logger.LogInformation("Bot Name: {BotName}", config.BotName);
        logger.LogInformation("API URL: {ApiUrl}", config.ApiUrl);
        if (!string.IsNullOrEmpty(config.TournamentId))
        {
            logger.LogInformation("Tournament ID: {TournamentId}", config.TournamentId);
        }

        using var tournamentClient = new TournamentClient(config, logger);
        await tournamentClient.RunAsync(cts.Token);
    }
    else
    {
        var logger = loggerFactory.CreateLogger<BattleshipsBot>();
        var apiUrl = Environment.GetEnvironmentVariable("GAME_API_URL") ?? "https://battleships.devrel.hny.wtf";
        var botName = Environment.GetEnvironmentVariable("BOT_NAME") ?? "csharp-shooter";

        logger.LogInformation("Starting in LEGACY MODE");
        var bot = new BattleshipsBot(apiUrl, botName, logger);
        await bot.RunAsync();
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Shutdown complete");
    return 0;
}
catch (Exception ex)
{
    var logger = loggerFactory.CreateLogger("Program");
    logger.LogError(ex, "Fatal error in bot");
    return 1;
}

return 0;
