using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using EdgeHunter.Skirmish;
using EdgeHunter.Skirmish.Models;

var serviceName = Environment.GetEnvironmentVariable("BOT_NAME") ?? "NavalGazing";
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
    var logger = loggerFactory.CreateLogger<SkirmishClient>();
    var config = SkirmishConfig.FromEnvironment();

    logger.LogInformation("Starting EdgeHunter bot in TOURNAMENT MODE");
    logger.LogInformation("Bot Name: {BotName}", config.BotName);
    logger.LogInformation("API URL: {ApiUrl}", config.ApiUrl);
    if (!string.IsNullOrEmpty(config.SkirmishId))
    {
        logger.LogInformation("Skirmish ID: {SkirmishId}", config.SkirmishId);
    }

    using var skirmishClient = new SkirmishClient(config, logger);
    await skirmishClient.RunAsync(cts.Token);
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
