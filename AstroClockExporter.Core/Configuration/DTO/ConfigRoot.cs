namespace AstroClockExporter.Core.Configuration.DTO;

[Serializable]
public sealed record ConfigRoot
{
    public IReadOnlyDictionary<string, Location> Locations { get; set; } = new Dictionary<string, Location>();
}
