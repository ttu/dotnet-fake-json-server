using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace FakeServer.Authentication.Basic
{
    public static class BasicAuthenticationConfiguration
    {
        public static IServiceCollection AddBasicAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(o =>
            {
                o.DefaultScheme = BasicAuthenticationDefaults.AuthenticationScheme;
                o.DefaultAuthenticateScheme = BasicAuthenticationDefaults.AuthenticationScheme;
            })
            .AddBasicAuthentication();

            return services;
        }
    }

    public static class BasicAuthenticationDefaults
    {
        public static string AuthenticationScheme => "Basic";
    }

    public static class BasicAuthenticationExtensions
    {
        public static AuthenticationBuilder AddBasicAuthentication(this AuthenticationBuilder builder)
               => builder.AddBasicAuthentication(BasicAuthenticationDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddBasicAuthentication(this AuthenticationBuilder builder, Action<BasicTokenOptions> configureOptions)
          => builder.AddBasicAuthentication(BasicAuthenticationDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddBasicAuthentication(this AuthenticationBuilder builder, string authenticationScheme, Action<BasicTokenOptions> configureOptions)
            => builder.AddBasicAuthentication(authenticationScheme, displayName: null, configureOptions: configureOptions);

        public static AuthenticationBuilder AddBasicAuthentication(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<BasicTokenOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<BasicTokenOptions>, BasicTokenPostConfigureOptions>());
            return builder.AddScheme<BasicTokenOptions, BasicAuthenticationHandler>(authenticationScheme, displayName, configureOptions);
        }
        public static OpenApiSecurityScheme GetBasicSecurityDefinition(this SwaggerGenOptions s)
        {
            return new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = BasicAuthenticationDefaults.AuthenticationScheme,
                In = ParameterLocation.Header
            };
        }
        public static OpenApiSecurityRequirement GetBasicSecurityRequirement(this SwaggerGenOptions s)
        {
            var securityRequirement = new OpenApiSecurityRequirement();
            var scheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = BasicAuthenticationDefaults.AuthenticationScheme
                }
            };

            securityRequirement.Add(scheme, new List<string>());

            return securityRequirement;
        }
    }

    public class BasicTokenPostConfigureOptions : IPostConfigureOptions<BasicTokenOptions>
    {
        public void PostConfigure(string name, BasicTokenOptions options)
        {
        }
    }

    public class BasicTokenOptions : AuthenticationSchemeOptions
    {
        public BasicTokenOptions() : base()
        {
        }

        public override void Validate()
        {
        }
    }

    public class BasicAuthenticationHandler : AuthenticationHandler<BasicTokenOptions>
    {
        // "Basic "
        private const int HeaderMinLength = 6;
        
        public BasicAuthenticationHandler(IOptionsMonitor<BasicTokenOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = Context.Request.Headers["Authorization"].ToString();

            bool Authenticate(out string name)
            {
                var authenticationSettings = Context.RequestServices.GetService(typeof(IOptions<AuthenticationSettings>)) as IOptions<AuthenticationSettings>;

                var token = authHeader.Substring(HeaderMinLength).Trim();
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
                authHeader.Length > HeaderMinLength &&
                Authenticate(out string loginName))
            {
                var claims = new[] { new Claim("name", loginName), new Claim(ClaimTypes.Role, "Admin") };
                var identity = new ClaimsIdentity(claims, BasicAuthenticationDefaults.AuthenticationScheme);

                return Task.FromResult(AuthenticateResult.Success(
                          new AuthenticationTicket(
                              new ClaimsPrincipal(identity),
                              new AuthenticationProperties(),
                              BasicAuthenticationDefaults.AuthenticationScheme)));
            }
            else
            {
                Context.Response.Headers["WWW-Authenticate"] = BasicAuthenticationDefaults.AuthenticationScheme;
                return Task.FromResult(AuthenticateResult.Fail(""));
            }
        }
    }
}