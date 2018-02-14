using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace FakeServer.Common
{
    public class HeadMethodMiddleware
    {
        private readonly RequestDelegate _next;

        public HeadMethodMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            if (context.Request.Method == HttpMethods.Head)
            {
                context.Response.Body = null;
            }
        }
    }
}