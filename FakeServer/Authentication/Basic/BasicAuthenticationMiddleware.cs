using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FakeServer.Authentication.Basic
{
    // Basic Authentication implemented with normal Middleware
    public class BasicAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public BasicAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();

            bool Authenticate(out string name)
            {
                var authenticationSettings = context.RequestServices.GetService(typeof(IOptions<AuthenticationSettings>)) as IOptions<AuthenticationSettings>;

                var token = authHeader.Substring("Basic ".Length).Trim();
                var credentialString = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var credentials = credentialString.Split(':');

                if (authenticationSettings.Value.Users.Any(e => e.Username == credentials[0] && e.Password == credentials[1]))
                {
                    name = credentials[0];
                    return true;
                }

                name = string.Empty;
                return false;
            };

            if (!string.IsNullOrEmpty(authHeader) &&
                authHeader.StartsWith("basic", StringComparison.OrdinalIgnoreCase) &&
                Authenticate(out string loginName))
            {
                var claims = new[] { new Claim("name", loginName), new Claim(ClaimTypes.Role, "Admin") };
                var identity = new ClaimsIdentity(claims, "Basic");
                context.User = new ClaimsPrincipal(identity);
            }
            else
            {
                context.Response.StatusCode = 401;
                context.Response.Headers["WWW-Authenticate"] = "Basic";
            }

            await _next(context);
        }
    }
}