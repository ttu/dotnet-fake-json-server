using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace FakeServer.Authentication.ApiKey
{
    public static class ApiKeyAuthenticationConfiguration
    {
        public static void AddApiKeyAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(o =>
                    {
                        o.DefaultScheme = ApiKeyAuthenticationDefaults.AuthenticationScheme;
                        o.DefaultAuthenticateScheme = ApiKeyAuthenticationDefaults.AuthenticationScheme;
                    })
                    .AddApiKeyAuthentication();
        }
    }

    public static class ApiKeyAuthenticationDefaults
    {
        public static string AuthenticationScheme => "ApiKey";
    }

    public static class BasicAuthenticationExtensions
    {
        public static AuthenticationBuilder AddApiKeyAuthentication(this AuthenticationBuilder builder)
            => builder.AddApiKeyAuthentication(ApiKeyAuthenticationDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddApiKeyAuthentication(this AuthenticationBuilder builder, Action<BasicTokenOptions> configureOptions)
            => builder.AddApiKeyAuthentication(ApiKeyAuthenticationDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddApiKeyAuthentication(this AuthenticationBuilder builder, string authenticationScheme, Action<BasicTokenOptions> configureOptions)
            => builder.AddApiKeyAuthentication(authenticationScheme, displayName: null, configureOptions: configureOptions);

        public static AuthenticationBuilder AddApiKeyAuthentication(this AuthenticationBuilder builder, string authenticationScheme, string displayName,
            Action<BasicTokenOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<BasicTokenOptions>, BasicTokenPostConfigureOptions>());
            return builder.AddScheme<BasicTokenOptions, BasicAuthenticationHandler>(authenticationScheme, displayName, configureOptions);
        }

        public static OpenApiSecurityScheme GetBasicSecurityDefinition(this SwaggerGenOptions s)
        {
            return new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http, Scheme = ApiKeyAuthenticationDefaults.AuthenticationScheme, In = ParameterLocation.Header
            };
        }

        public static OpenApiSecurityRequirement GetBasicSecurityRequirement(this SwaggerGenOptions s)
        {
            var securityRequirement = new OpenApiSecurityRequirement();
            var scheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme, Id = ApiKeyAuthenticationDefaults.AuthenticationScheme
                }
            };

            securityRequirement.Add(scheme, new List<string>());

            return securityRequirement;
        }
    }

    public class BasicTokenPostConfigureOptions : IPostConfigureOptions<BasicTokenOptions>
    {
        public void PostConfigure(string name, BasicTokenOptions options)
        { }
    }

    public class BasicTokenOptions : AuthenticationSchemeOptions
    {
        public BasicTokenOptions() : base()
        { }

        public override void Validate()
        { }
    }

    public class BasicAuthenticationHandler : AuthenticationHandler<BasicTokenOptions>
    {
        public BasicAuthenticationHandler(IOptionsMonitor<BasicTokenOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = Context.Request.Headers["X-API-Key"].ToString();

            var authenticationSettings = Context.RequestServices.GetService(typeof(IOptions<AuthenticationSettings>)) as IOptions<AuthenticationSettings>;

            if (authenticationSettings.Value.ApiKey == authHeader)
            {
                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(
                        new ClaimsPrincipal(new ClaimsIdentity("Custom")),
                        new AuthenticationProperties(),
                        ApiKeyAuthenticationDefaults.AuthenticationScheme)));
            }
            else
            {
                Context.Response.Headers["WWW-Authenticate"] = ApiKeyAuthenticationDefaults.AuthenticationScheme;
                return Task.FromResult(AuthenticateResult.Fail(""));
            }
        }
    }
}