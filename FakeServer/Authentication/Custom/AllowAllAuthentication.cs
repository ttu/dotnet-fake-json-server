using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FakeServer.Authentication.Custom;

public static class AllowAllAuthenticationConfiguration
{
    public static IServiceCollection AddAllowAllAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(o => { o.DefaultAuthenticateScheme = AllowAllAuthenticationDefaults.AuthenticationScheme; })
            .AddAllowAllAuthentication();

        return services;
    }
}

public static class AllowAllAuthenticationDefaults
{
    public static string AuthenticationScheme => string.Empty;
}

public static class AllowAllAuthenticationExtensions
{
    public static AuthenticationBuilder AddAllowAllAuthentication(this AuthenticationBuilder builder)
        => builder.AddAllowAllAuthentication(AllowAllAuthenticationDefaults.AuthenticationScheme, _ => { });

    public static AuthenticationBuilder AddAllowAllAuthentication(this AuthenticationBuilder builder, Action<AllowAllOptions> configureOptions)
        => builder.AddAllowAllAuthentication(AllowAllAuthenticationDefaults.AuthenticationScheme, configureOptions);

    public static AuthenticationBuilder AddAllowAllAuthentication(this AuthenticationBuilder builder, string authenticationScheme,
        Action<AllowAllOptions> configureOptions)
        => builder.AddAllowAllAuthentication(authenticationScheme, displayName: null, configureOptions: configureOptions);

    public static AuthenticationBuilder AddAllowAllAuthentication(this AuthenticationBuilder builder, string authenticationScheme, string displayName,
        Action<AllowAllOptions> configureOptions)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<AllowAllOptions>, AllowAllPostConfigureOptions>());
        return builder.AddScheme<AllowAllOptions, AllowAllAuthenticationHandler>(authenticationScheme, displayName, configureOptions);
    }
}

public class AllowAllPostConfigureOptions : IPostConfigureOptions<AllowAllOptions>
{
    public void PostConfigure(string name, AllowAllOptions options)
    {
    }
}

public class AllowAllOptions : AuthenticationSchemeOptions
{
    public AllowAllOptions() : base()
    {
    }

    public override void Validate()
    {
    }
}

public class AllowAllAuthenticationHandler : AuthenticationHandler<AllowAllOptions>
{
    public AllowAllAuthenticationHandler(IOptionsMonitor<AllowAllOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return await Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity("Custom")),
                new AuthenticationProperties(),
                AllowAllAuthenticationDefaults.AuthenticationScheme)));
    }
}