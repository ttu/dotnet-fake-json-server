using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace FakeServer.Authentication.ApiKey;

public static class ApiKeyAuthenticationConfiguration
{
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(o =>
            {
                o.DefaultScheme = ApiKeyAuthenticationDefaults.AuthenticationScheme;
                o.DefaultAuthenticateScheme = ApiKeyAuthenticationDefaults.AuthenticationScheme;
            })
            .AddApiKeyAuthentication();

        return services;
    }
}

public static class ApiKeyAuthenticationDefaults
{
    public static string AuthenticationScheme => "apiKey";
}

public static class ApiKeyAuthenticationExtensions
{
    public static AuthenticationBuilder AddApiKeyAuthentication(this AuthenticationBuilder builder)
        => builder.AddApiKeyAuthentication(ApiKeyAuthenticationDefaults.AuthenticationScheme, _ => { });

    public static AuthenticationBuilder AddApiKeyAuthentication(this AuthenticationBuilder builder, Action<ApiKeyOptions> configureOptions)
        => builder.AddApiKeyAuthentication(ApiKeyAuthenticationDefaults.AuthenticationScheme, configureOptions);

    public static AuthenticationBuilder AddApiKeyAuthentication(this AuthenticationBuilder builder, string authenticationScheme,
        Action<ApiKeyOptions> configureOptions)
        => builder.AddApiKeyAuthentication(authenticationScheme, displayName: null, configureOptions: configureOptions);

    public static AuthenticationBuilder AddApiKeyAuthentication(this AuthenticationBuilder builder, string authenticationScheme, string displayName,
        Action<ApiKeyOptions> configureOptions)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<ApiKeyOptions>, ApiKeyPostConfigureOptions>());
        return builder.AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(authenticationScheme, displayName, configureOptions);
    }

    public static OpenApiSecurityScheme GetApiKeySecurityDefinition(this SwaggerGenOptions s)
    {
        return new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey, Scheme = ApiKeyAuthenticationDefaults.AuthenticationScheme, In = ParameterLocation.Header, Name = "X-API-KEY"
        };
    }

    public static OpenApiSecurityRequirement GetApiKeySecurityRequirement(this SwaggerGenOptions s)
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

public class ApiKeyPostConfigureOptions : IPostConfigureOptions<ApiKeyOptions>
{
    public void PostConfigure(string name, ApiKeyOptions options)
    {
    }
}

public class ApiKeyOptions : AuthenticationSchemeOptions
{
    public ApiKeyOptions() : base()
    {
    }

    public override void Validate()
    {
    }
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyOptions>
{
    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Context.Request.Headers["X-API-KEY"].ToString();

        var authenticationSettings = Context.RequestServices.GetService(typeof(IOptions<AuthenticationSettings>)) as IOptions<AuthenticationSettings>;

        if (authenticationSettings!.Value.ApiKey == authHeader)
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