using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FakeServer.Common
{
    //  Based on middleware from: https://gist.github.com/madskristensen/36357b1df9ddbfd123162cd4201124c4

    public class ETagMiddleware
    {
        private readonly RequestDelegate _next;

        public ETagMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Value.StartsWith($"/{Config.ApiRoute}") && context.Request.Method == "GET")
            {
                var response = context.Response;
                var originalStream = response.Body;

                using (var ms = new MemoryStream())
                {
                    response.Body = ms;

                    await _next(context);

                    if (IsEtagSupported(response))
                    {
                        var checksum = CalculateChecksum(ms);

                        response.Headers[HeaderNames.ETag] = checksum;

                        if (context.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag) && checksum == etag)
                        {
                            response.StatusCode = StatusCodes.Status304NotModified;
                            return;
                        }
                    }

                    ms.Position = 0;
                    await ms.CopyToAsync(originalStream);
                }
            }
            else
            {
                await _next(context);
            }
        }

        private static bool IsEtagSupported(HttpResponse response)
        {
            if (response.StatusCode != StatusCodes.Status200OK)
                return false;

            // The 20kb length limit is not based in science. Feel free to change
            if (response.Body.Length > 20 * 1024)
                return false;

            if (response.Headers.ContainsKey(HeaderNames.ETag))
                return false;

            return true;
        }

        private static string CalculateChecksum(MemoryStream ms)
        {
            var checksum = string.Empty;

            using (var algo = SHA1.Create())
            {
                ms.Position = 0;
                var bytes = algo.ComputeHash(ms);
                checksum = $"\"{WebEncoders.Base64UrlEncode(bytes)}\"";
            }

            return checksum;
        }
    }
}