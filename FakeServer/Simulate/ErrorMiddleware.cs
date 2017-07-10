using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FakeServer.Simulate
{
    public class ErrorMiddleware
    {
        private readonly ErrorSettings _settings;
        private readonly RequestDelegate _next;
        private readonly string[] _skipwords = new string[] { ".html", ".ico", "swagger", "ws" };

        public ErrorMiddleware(RequestDelegate next, IOptions<SimulateSettings> settings)
        {
            _next = next;
            _settings = settings.Value.Error;
        }

        public async Task Invoke(HttpContext context)
        {
            bool skipCheck = context.Request.Path == "/" || _skipwords.Any(context.Request.Path.ToString().ToLower().Contains);

            if (!skipCheck && _settings.Methods.Contains(context.Request.Method))
            {
                if (_settings.Probability >= new Random().Next(1, 100))
                    throw new Exception("ErrorMiddleware");
            }

            await _next(context);
        }
    }
}