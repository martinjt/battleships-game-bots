using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using HeatSeeker.Tournament;
using HeatSeeker.Tournament.Models;

var serviceName = Environment.GetEnvironmentVariable("BOT_NAME") ?? "SunkCost";
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

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\nShutting down gracefully...");
};

try
{
    var logger = loggerFactory.CreateLogger<TournamentClient>();
    var config = TournamentConfig.FromEnvironment();

    logger.LogInformation("Starting HeatSeeker bot in TOURNAMENT MODE");
    logger.LogInformation("Bot Name: {BotName}", config.BotName);
    logger.LogInformation("API URL: {ApiUrl}", config.ApiUrl);
    if (!string.IsNullOrEmpty(config.TournamentId))
    {
        logger.LogInformation("Tournament ID: {TournamentId}", config.TournamentId);
    }

    using var tournamentClient = new TournamentClient(config, logger);
    await tournamentClient.RunAsync(cts.Token);
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
