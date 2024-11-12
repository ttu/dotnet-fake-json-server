using FakeServer.Authentication.ApiKey;
using FakeServer.Authentication.Basic;
using FakeServer.Authentication.Custom;
using FakeServer.Authentication.Jwt;

namespace FakeServer.Authentication;

static class AuthenticationConfiguration
{
    public static AuthenticationType ReadType(IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("Authentication:Enabled") == false)
            return AuthenticationType.AllowAll;

        var authenticationType = configuration["Authentication:AuthenticationType"].ToLower();

        return authenticationType switch
        {
            "token" => AuthenticationType.JwtBearer,
            "basic" => AuthenticationType.Basic,
            "apikey" => AuthenticationType.ApiKey,
            _ => throw new ArgumentException($"Invalid authentication type: {authenticationType}"),
        };
    }

    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, AuthenticationType type)
    {
        return type switch
        {
            AuthenticationType.AllowAll => services.AddAllowAllAuthentication(),
            AuthenticationType.JwtBearer => services.AddJwtBearerAuthentication(),
            AuthenticationType.Basic => services.AddBasicAuthentication(),
            AuthenticationType.ApiKey => services.AddApiKeyAuthentication(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Not a valid authentication type")
        };
    }
}