using AstroClockExporter.Core.Common;

namespace AstroClockExporter.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_StoresValueAndMarksSuccess()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFail);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Err);
    }

    [Fact]
    public void Fail_WithMessage_HasCodeZeroAndMessage()
    {
        var result = Result<int>.Fail("boom");

        Assert.True(result.IsFail);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Err);
        Assert.Equal(0, result.Err.Code);
        Assert.Equal("boom", result.Err.Message);
    }

    [Fact]
    public void Fail_WithCode_StoresCodeAndMessage()
    {
        var result = Result<string>.Fail(404, "not found");

        Assert.True(result.IsFail);
        Assert.NotNull(result.Err);
        Assert.Equal(404, result.Err.Code);
        Assert.Equal("not found", result.Err.Message);
    }

    [Fact]
    public void Success_OfReferenceType_StoresInstance()
    {
        var payload = new object();
        var result = Result<object>.Success(payload);

        Assert.Same(payload, result.Value);
    }
}
