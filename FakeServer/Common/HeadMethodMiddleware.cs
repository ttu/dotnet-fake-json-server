using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FakeServer.Common
{
    // ref: https://www.tpeczek.com/2017/10/exploring-head-method-behavior-in.html
    public class HeadMethodMiddleware
    {
        private readonly RequestDelegate _next;

        public HeadMethodMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            bool methodSwitched = false;

            if (HttpMethods.IsHead(context.Request.Method))
            {
                context.Request.Method = HttpMethods.Get;
                context.Response.Body = Stream.Null;

                methodSwitched = true;
            }

            await _next.Invoke(context);

            if (methodSwitched)
            {
                context.Request.Method = HttpMethods.Head;
            }
        }
    }

    // ref: https://www.tutorialsteacher.com/core/how-to-add-custom-middleware-aspnet-core
    public static class HeadMethodMiddlewareExtensions
    {
        public static IApplicationBuilder UseHeadMethodMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HeadMethodMiddleware>();
        }
    }
}
