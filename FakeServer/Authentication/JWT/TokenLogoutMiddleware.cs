using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace FakeServer.Authentication.Jwt
{
    public class TokenBlacklistService
    {
        private readonly List<string> _headers = new List<string>();

        public void BlacklistHeader(string header)
        {
            if (GetJtiFromToken(header, out var jti))
                _headers.Add(jti);
        }

        public bool IsBlacklisted(string header) => GetJtiFromToken(header, out var jti) ? _headers.Contains(jti) : false;

        private bool GetJtiFromToken(string header, out string jti)
        {
            header = header.Replace("Bearer ", "");

            var jsonToken = new JwtSecurityTokenHandler().ReadToken(header) as JwtSecurityToken;
            jti = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == "jti")?.Value;
            return !string.IsNullOrEmpty(jti);
        }
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
            blacklistService.BlacklistHeader(header.ToString());

            return Task.FromResult(0);
        }
    }
}