using AstroClockExporter.Core.Configuration.DTO;

namespace AstroClockExporter.Core.Astro;

public interface IAstroCalculator
{
    AstroReading Calculate(Location location, DateTime utcNow);
}
