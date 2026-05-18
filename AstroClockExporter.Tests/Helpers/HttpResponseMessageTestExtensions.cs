using System.Net;
using System.Text.RegularExpressions;

namespace AstroClockExporter.Tests.Helpers;

internal static class HttpResponseMessageTestExtensions
{
    extension(HttpResponseMessage response)
    {
        public void EnsureStatusCode(HttpStatusCode statusCode, string contentType)
        {
            Assert.Equal(statusCode, response.StatusCode);
            Assert.Equal($"{contentType}; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        public async Task EnsureResponseIs(string expectedContent)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedContent, content);
        }

        public async Task EnsureResponseMatches(Regex expectedContent)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Matches(expectedContent, content);
        }
    }
}
