using FakeServer.Common;
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

            if (context.Request.Path.Value.StartsWith($"/{Config.ApiRoute}") &&
                _udpateMethods.Contains(context.Request.Method) &&
                (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300))
            {
                var method = context.Request.Method;
                var path = context.Request.Path.Value;

                if (method == "POST")
                {
                    var location = context.Response.Headers["Location"].ToString();
                    var itemId = location.Substring(location.LastIndexOf('/') + 1);
                    path = $"{path}/{itemId}";
                }

                var data = ObjectHelper.GetWebSocketMessage(method, path);
                _bus.Publish("updated", data);
            }
        }
    }
}