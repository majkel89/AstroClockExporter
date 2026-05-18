using System.Text;
using AstroClockExporter.Api;
using Microsoft.AspNetCore.Http;

namespace AstroClockExporter.Tests.Api;

public class HttpContextExtensionsTests
{
    [Fact]
    public async Task SendTextAsync_DefaultStatus_WritesBodyAndContentType()
    {
        var ctx = CreateContext(out var body);

        await ctx.SendTextAsync("hello world");

        Assert.Equal(StatusCodes.Status200OK, ctx.Response.StatusCode);
        Assert.Equal("text/plain; charset=utf-8", ctx.Response.ContentType);
        Assert.Equal("hello world", ReadBody(body));
    }

    [Fact]
    public async Task SendTextAsync_ExplicitStatus_PropagatesStatusCode()
    {
        var ctx = CreateContext(out var body);

        await ctx.SendTextAsync("nope", StatusCodes.Status404NotFound);

        Assert.Equal(StatusCodes.Status404NotFound, ctx.Response.StatusCode);
        Assert.Equal("nope", ReadBody(body));
    }

    [Fact]
    public async Task SendTextAsync_EmptyMessage_WritesZeroBytes()
    {
        var ctx = CreateContext(out var body);

        await ctx.SendTextAsync(string.Empty);

        Assert.Equal(StatusCodes.Status200OK, ctx.Response.StatusCode);
        Assert.Equal(0, body.Length);
    }

    private static DefaultHttpContext CreateContext(out MemoryStream body)
    {
        body = new MemoryStream();
        var ctx = new DefaultHttpContext
        {
            Response =
            {
                Body = body
            }
        };
        return ctx;
    }

    private static string ReadBody(MemoryStream body)
    {
        body.Position = 0;
        return Encoding.UTF8.GetString(body.ToArray());
    }
}
