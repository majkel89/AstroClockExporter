using AstroClockExporter.Core.Common;
using AstroClockExporter.Core.Configuration.DTO;

namespace AstroClockExporter.Core;

public interface IConfigProvider
{
    Task LoadAsync(string filePath, CancellationToken ct);

    ConfigRoot GetConfigRoot();

    Result<Location> GetLocation(string locationName);
}
