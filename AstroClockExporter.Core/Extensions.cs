using AstroClockExporter.Core.Astro;
using AstroClockExporter.Core.Configuration;
using AstroClockExporter.Core.Configuration.Serialization;
using AstroClockExporter.Core.Prometheus;
using Microsoft.Extensions.DependencyInjection;

namespace AstroClockExporter.Core;

public static class Extensions
{
    public static void AddCore(this IServiceCollection services)
    {
        services.AddSingleton<IConfigReader, YamlConfigReader>();
        services.AddSingleton<IConfigProvider, ConfigProvider>();
        services.AddSingleton<IAstroCalculator, AstroCalculator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IMetricsExporter, PrometheusMetricsExporter>();
    }
}
