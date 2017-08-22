using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace FakeServer.Authentication.Basic
{
    public class BasicTokenOptions : AuthenticationOptions
    {
        public BasicTokenOptions() : base()
        {
            AuthenticationScheme = "Basic";
            AutomaticAuthenticate = true;
            AutomaticChallenge = true;
        }
    }

    public class BasicAuthenticationMiddleware : AuthenticationMiddleware<BasicTokenOptions>
    {
        public BasicAuthenticationMiddleware(RequestDelegate next,
                                IOptions<BasicTokenOptions> options,
                                ILoggerFactory loggerFactory,
                                UrlEncoder encoder)
                                : base(next, options, loggerFactory, encoder)
        {
        }

        protected override AuthenticationHandler<BasicTokenOptions> CreateHandler()
        {
            return new BasicAuthenticationHandler();
        }
    }

    public class BasicAuthenticationHandler : AuthenticationHandler<BasicTokenOptions>
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = Context.Request.Headers["Authorization"].ToString();

            bool Authenticate(out string name)
            {
                var authenticationSettings = Context.RequestServices.GetService(typeof(IOptions<AuthenticationSettings>)) as IOptions<AuthenticationSettings>;

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

                return Task.FromResult(AuthenticateResult.Success(
                          new AuthenticationTicket(
                              new ClaimsPrincipal(identity),
                              new AuthenticationProperties(),
                              Options.AuthenticationScheme)));
            }
            else
            {
                Context.Response.Headers["WWW-Authenticate"] = "Basic";
                return Task.FromResult(AuthenticateResult.Fail(""));
            }
        }
    }
}