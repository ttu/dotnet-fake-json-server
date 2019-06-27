using System.IO;
using System.Threading.Tasks;
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
            if (!HttpMethods.IsHead(context.Request.Method))
            {
                await _next.Invoke(context);
                return;
            }

            context.Request.Method = HttpMethods.Get;
            context.Response.Body = Stream.Null;

            await _next.Invoke(context);

            context.Request.Method = HttpMethods.Head;
        }
    }
}
