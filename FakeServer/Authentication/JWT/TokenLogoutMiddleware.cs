using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FakeServer.Authentication.Jwt
{
    public class TokenBlacklistService
    {
        public List<string> Headers { get; } = new List<string>();
    }

    public class TokenLogoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenProviderOptions _options;

        public TokenLogoutMiddleware(
            RequestDelegate next,
            IOptions<TokenProviderOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        public Task Invoke(HttpContext context)
        {
            // If the request path doesn't match, skip
            if (!context.Request.Path.Equals(_options.LogoutPath, StringComparison.Ordinal))
            {
                return _next(context);
            }

            if (!context.Request.Method.Equals("POST"))
            {
                context.Response.StatusCode = 400;
                return context.Response.WriteAsync("Bad request.");
            }

            var blacklistService = context.RequestServices.GetService(typeof(TokenBlacklistService)) as TokenBlacklistService;

            var header = context.Request.Headers["Authorization"];
            blacklistService.Headers.Add(header.ToString());

            return Task.FromResult(0);
        }
    }
}