using AstroClockExporter.Core.Common;

namespace AstroClockExporter.Core;

public interface IMetricsExporter
{
    Result<string> ExportMetrics(string locationName);
}
