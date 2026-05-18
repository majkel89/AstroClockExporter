using AstroClockExporter.Core.Configuration.DTO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AstroClockExporter.Core.Configuration.Serialization;

internal sealed class YamlConfigReader : IConfigReader
{
    private static readonly IDeserializer Deserializer =
        new StaticDeserializerBuilder(new YamlConfigStaticContext())
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

    public ConfigRoot? ReadConfigFromString(string contents) =>
        Deserializer.Deserialize<ConfigRoot?>(contents);
}
