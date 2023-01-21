using Microsoft.Extensions.Options;

namespace FakeServer.Simulate;

public class ErrorMiddleware
{
    private readonly ErrorSettings _settings;
    private readonly RequestDelegate _next;
    private readonly string[] _skipWords = { ".html", ".ico", "swagger", "ws" };

    public ErrorMiddleware(RequestDelegate next, IOptions<SimulateSettings> settings)
    {
        _next = next;
        _settings = settings.Value.Error;
    }

    public async Task Invoke(HttpContext context)
    {
        var skipCheck = context.Request.Path == "/" || _skipWords.Any(context.Request.Path.ToString().ToLower().Contains);

        if (!skipCheck && _settings.Methods.Contains(context.Request.Method))
        {
            if (_settings.Probability >= new Random().Next(1, 100))
                throw new Exception("ErrorMiddleware");
        }

        await _next(context);
    }
}