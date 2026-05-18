using AstroClockExporter.Api;
using AstroClockExporter.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.ColorBehavior = LoggerColorBehavior.Enabled;
});

builder.Services.AddCore();

var app = builder.Build();

var cts = new CancellationTokenSource();
var configFile = Path.GetFullPath(Environment.GetEnvironmentVariable("CONFIG_FILE") ?? "config.yml");
await app.Services.GetRequiredService<IConfigProvider>().LoadAsync(configFile, cts.Token);

app.MapGet("/", async ctx =>
{
    await ctx.SendTextAsync("AstroClockExporter");
});

app.MapGet("/healthy", async ctx =>
{
    await ctx.SendTextAsync("healthy");
});

app.MapGet("/metrics", async ctx =>
{
    var location = ctx.Request.Query["location"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(location))
    {
        await ctx.SendTextAsync("Parameter location is required", StatusCodes.Status400BadRequest);
        return;
    }

    var exporter = ctx.RequestServices.GetRequiredService<IMetricsExporter>();
    var result = exporter.ExportMetrics(location.Trim());

    if (result.IsFail)
    {
        await ctx.SendTextAsync(result.Err.Message, result.Err.Code);
        return;
    }

    await ctx.SendTextAsync(result.Value);
});

app.Run();

[UsedImplicitly]
[ExcludeFromDescription]
public partial class Program;
