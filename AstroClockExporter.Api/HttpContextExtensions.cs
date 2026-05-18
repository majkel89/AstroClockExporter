namespace AstroClockExporter.Api;

internal static class HttpContextExtensions
{
    public static async Task SendTextAsync(this HttpContext ctx, string message, int statusCode = StatusCodes.Status200OK)
    {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.Response.WriteAsync(message, ctx.RequestAborted);
    }
}
