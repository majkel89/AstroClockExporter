using AstroClockExporter.Core.Common;

namespace AstroClockExporter.Tests.Common;

public class ErrorTests
{
    [Fact]
    public void New_WithMessage_DefaultsCodeToZero()
    {
        var error = Error.New("oops");

        Assert.Equal(0, error.Code);
        Assert.Equal("oops", error.Message);
    }

    [Fact]
    public void New_WithCode_StoresCodeAndMessage()
    {
        var error = Error.New(500, "kaboom");

        Assert.Equal(500, error.Code);
        Assert.Equal("kaboom", error.Message);
    }

    [Fact]
    public void ToString_ReturnsMessage()
    {
        var error = Error.New(418, "I'm a teapot");

        Assert.Equal("I'm a teapot", error.ToString());
    }
}
