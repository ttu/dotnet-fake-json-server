using System.Text.RegularExpressions;

namespace FakeServer.Common;

public class HttpOptionsMiddleware
{
    private readonly RequestDelegate _next;

    private readonly Dictionary<string, string> _routeMethods = new Dictionary<string, string>
    {
        { $@"^\/{Config.ApiRoute}\/?$", "GET, HEAD, POST, OPTIONS" }, // /api
        { $@"^\/{Config.ApiRoute}\/\w+\/?$", "GET, HEAD, POST, OPTIONS" }, // /api/{collection}
        { $@"^\/{Config.ApiRoute}\/\w+\/.*", "GET, HEAD, POST, PUT, PATCH, DELETE, OPTIONS" }, // /api/{collection}/{id}
        { $@"^\/{Config.AsyncRoute}\/queue\/.*", "GET, DELETE, OPTIONS" }, // /async/queue/{id}
        { $@"^\/{Config.AsyncRoute}\/\w+\/?$", "POST, OPTIONS" }, // /async/{collection}
        { $@"^\/{Config.AsyncRoute}\/\w+\/.*", "PUT, PATCH, DELETE, OPTIONS" }, // /async/{collection}/{id}
    };

    public HttpOptionsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Method == HttpMethod.Options.Method)
        {
            var methods = _routeMethods.FirstOrDefault(e => Regex.IsMatch(context.Request.Path.Value, e.Key));
            context.Response.Headers.Add("Allow", new[] { methods.Value });
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("OK");
        }
        else
        {
            await _next(context);
        }
    }
}