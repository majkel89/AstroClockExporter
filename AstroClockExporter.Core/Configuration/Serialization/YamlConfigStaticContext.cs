using System.Diagnostics.CodeAnalysis;
using AstroClockExporter.Core.Configuration.DTO;
using YamlDotNet.Serialization;

namespace AstroClockExporter.Core.Configuration.Serialization;

[ExcludeFromCodeCoverage]
[YamlStaticContext]
[YamlSerializable(typeof(ConfigRoot))]
[YamlSerializable(typeof(Location))]
[YamlSerializable(typeof(IDictionary<string, Location>))]
public partial class YamlConfigStaticContext;
