using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace FakeServer.WebSockets
{
    public class WebSocketMiddleware
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        private readonly JsonSerializerSettings _camelCaseSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly RequestDelegate _next;
        private readonly CancellationToken _token = CancellationToken.None;

        public WebSocketMiddleware(RequestDelegate next, IMessageBus bus)
        {
            _next = next;
            bus.Subscribe<dynamic>("updated", (message) =>
            {
                _sockets.Values
                    .Where(socket => socket.State == WebSocketState.Open)
                    .ToList()
                    .ForEach(async socket =>
                    {
                        var text = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, _camelCaseSettings));
                        var buffer = new ArraySegment<byte>(text);
                        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, _token);
                    });
            });
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next(context);
                return;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            _sockets.TryAdd(webSocket.GetHashCode().ToString(), webSocket);

            while (webSocket.State == WebSocketState.Open)
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);
                var received = await webSocket.ReceiveAsync(buffer, _token);

                switch (received.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _token);
                        _sockets.TryRemove(webSocket.GetHashCode().ToString(), out var toRemove);
                        return;
                }
            }
        }
    }
}