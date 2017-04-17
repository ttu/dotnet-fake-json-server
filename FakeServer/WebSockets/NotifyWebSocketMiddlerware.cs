using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FakeServer.WebSockets
{
    public class NotifyWebSocketMiddlerware
    {
        private readonly List<string> _udpateMethods = new List<string> { "POST", "PUT", "PATCH", "DELETE" };

        private readonly RequestDelegate _next;
        private readonly IMessageBus _bus;

        public NotifyWebSocketMiddlerware(RequestDelegate next, IMessageBus bus)
        {
            _next = next;
            _bus = bus;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            if (context.Request.Path.Value.StartsWith("/api") &&
                _udpateMethods.Contains(context.Request.Method) &&
                context.Response.StatusCode == 200)
            {
                var data = new { Method = context.Request.Method, Path = context.Request.Path.Value };
                _bus.Publish("updated", data);
            }
        }
    }
}