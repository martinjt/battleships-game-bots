using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackOverflowAttack;
using StackOverflowAttack.Skirmish;
using StackOverflowAttack.Skirmish.Models;

// Configure OpenTelemetry
var serviceName = Environment.GetEnvironmentVariable("BOT_NAME") ?? "StackOverflowAttack";
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? "http://otel-collector.battleships.svc.cluster.local:4317";

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
    .AddSource(serviceName)
    .AddHttpClientInstrumentation()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri(otlpEndpoint);
        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    })
    .Build();

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

// Check if tournament mode is enabled
var skirmishMode = Environment.GetEnvironmentVariable("SKIRMISH_MODE")?.ToLowerInvariant() == "true";

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
    if (skirmishMode)
    {
        var logger = loggerFactory.CreateLogger<SkirmishClient>();
        var config = SkirmishConfig.FromEnvironment();

        logger.LogInformation("Starting in SKIRMISH MODE");
        logger.LogInformation("Bot Name: {BotName}", config.BotName);
        logger.LogInformation("API URL: {ApiUrl}", config.ApiUrl);
        if (!string.IsNullOrEmpty(config.SkirmishId))
        {
            logger.LogInformation("Skirmish ID: {SkirmishId}", config.SkirmishId);
        }

        using var skirmishClient = new SkirmishClient(config, logger);
        await skirmishClient.RunAsync(cts.Token);
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
