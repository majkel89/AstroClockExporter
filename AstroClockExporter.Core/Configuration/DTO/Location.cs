namespace AstroClockExporter.Core.Configuration.DTO;

[Serializable]
public sealed record Location
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Elevation { get; set; }
}
