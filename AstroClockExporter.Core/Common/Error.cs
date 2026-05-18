namespace AstroClockExporter.Core.Common;

public record Error(int Code, string Message)
{
    public static Error New(string message) => new(0, message);
    public static Error New(int code, string message) => new(code, message);

    public override string ToString() => Message;
}
