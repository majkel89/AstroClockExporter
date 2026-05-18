using System.Runtime.CompilerServices;

namespace AstroClockExporter.Tests;

internal static class VerifyConfig
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.DontScrubDateTimes();
    }
}
