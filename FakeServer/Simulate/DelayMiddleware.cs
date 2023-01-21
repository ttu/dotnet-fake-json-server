using Microsoft.Extensions.Options;

namespace FakeServer.Simulate
{
    public class DelayMiddleware
    {
        private readonly DelaySettings _settings;
        private readonly RequestDelegate _next;

        public DelayMiddleware(RequestDelegate next, IOptions<SimulateSettings> settings)
        {
            _next = next;
            _settings = settings.Value.Delay;
        }

        public async Task Invoke(HttpContext context)
        {
            if (_settings.Methods.Contains(context.Request.Method))
            {
                await Task.Delay(new Random().Next(_settings.MinMs, _settings.MaxMs));
            }

            await _next(context);
        }
    }
}