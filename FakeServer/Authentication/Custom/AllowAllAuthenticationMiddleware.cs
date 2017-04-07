using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace FakeServer.Authentication.Custom
{
    public class TokenOptions : AuthenticationOptions
    {
        public TokenOptions() : base()
        {
            AuthenticationScheme = string.Empty;
            AutomaticAuthenticate = true;
            AutomaticChallenge = true;
        }
    }

    public class AllowAllAuthenticationMiddleware : AuthenticationMiddleware<TokenOptions>
    {
        public AllowAllAuthenticationMiddleware(RequestDelegate next,
                                IOptions<TokenOptions> options,
                                ILoggerFactory loggerFactory,
                                UrlEncoder encoder)
                                : base(next, options, loggerFactory, encoder)
        {
        }

        protected override AuthenticationHandler<TokenOptions> CreateHandler()
        {
            return new AuthHandler();
        }
    }

    public class AuthHandler : AuthenticationHandler<TokenOptions>
    {
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return await Task.FromResult(AuthenticateResult.Success(
                            new AuthenticationTicket(
                                new ClaimsPrincipal(new ClaimsIdentity("Custom")),
                                new AuthenticationProperties(), 
                                Options.AuthenticationScheme)));
        }
    }
}