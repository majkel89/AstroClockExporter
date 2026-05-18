using AstroClockExporter.Core.Configuration.DTO;

namespace AstroClockExporter.Core;

public interface IConfigReader
{
    ConfigRoot? ReadConfigFromString(string contents);
}
