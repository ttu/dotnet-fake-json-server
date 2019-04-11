using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FakeServer.Common
{
    // Based on the middleware from:
    // https://gist.github.com/madskristensen/36357b1df9ddbfd123162cd4201124c4

    public class ETagMiddleware
    {
        private readonly RequestDelegate _next;

        public ETagMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Etag Middleware can handle
            // GET : Caching of unchanged resources
            // PUT : Avoiding mid-air collisions

            if (context.Request.Path.Value.StartsWith($"/{Config.ApiRoute}") == false ||
                (context.Request.Method != HttpMethods.Get &&
                 context.Request.Method != HttpMethods.Head &&
                 context.Request.Method != HttpMethods.Put))
            {
                await _next(context);
                return;
            }

            if (context.Request.Method == HttpMethods.Get || context.Request.Method == HttpMethods.Head)
            {
                await HandleGet(context);
            }
            else
            {
                await HandlePut(context);
            }
        }

        private async Task HandleGet(HttpContext context)
        {
            // FrameResponseStream doesn't support reading, so we need to use MemoryStream as a buffer

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

        private async Task HandlePut(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey(HeaderNames.IfMatch))
            {
                // Switch request to GET and fetch data that is going to be updated
                // Compare reveived data's checksum to tag in If-Match header

                context.Request.Method = HttpMethods.Get;

                var response = context.Response;
                var originalStream = response.Body;

                using (var ms = new MemoryStream())
                {
                    response.Body = ms;

                    await _next(context);

                    if (IsEtagSupported(response))
                    {
                        if (context.Request.Headers.TryGetValue(HeaderNames.IfMatch, out var etag) && CalculateChecksum(ms) != etag)
                        {
                            context.Request.Method = HttpMethods.Put;
                            response.Body = originalStream;
                            response.StatusCode = StatusCodes.Status412PreconditionFailed;
                            return;
                        }
                    }

                    context.Request.Method = HttpMethods.Put;
                    response.Body = originalStream;
                }
            }

            await _next(context);
        }

        private static bool IsEtagSupported(HttpResponse response)
        {
            if (response.StatusCode != StatusCodes.Status200OK)
                return false;

            // The 2000kb length limit is not based in science. Feel free to change
            if (response.Body.Length > 2000 * 1024)
                return false;

            if (response.Headers.ContainsKey(HeaderNames.ETag))
                return false;

            return true;
        }

        private static string CalculateChecksum(MemoryStream ms)
        {
            using (var algo = SHA1.Create())
            {
                ms.Position = 0;
                var bytes = algo.ComputeHash(ms);
                return $"\"{WebEncoders.Base64UrlEncode(bytes)}\"";
            }
        }
    }
}