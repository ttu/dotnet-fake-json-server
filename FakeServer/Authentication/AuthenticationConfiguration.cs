using System;
using FakeServer.Authentication.ApiKey;
using FakeServer.Authentication.Basic;
using FakeServer.Authentication.Custom;
using FakeServer.Authentication.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FakeServer.Authentication
{
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
            switch (type)
            {
                case AuthenticationType.AllowAll:
                    services.AddAllowAllAuthentication();
                    break;
                case AuthenticationType.JwtBearer:
                    services.AddJwtBearerAuthentication();
                    break;
                case AuthenticationType.Basic:
                    services.AddBasicAuthentication();
                    break;
                case AuthenticationType.ApiKey:
                    services.AddApiKeyAuthentication();
                    break;
                default: throw new ArgumentException(nameof(type) + " is not a valid authentication type");
            }

            return services;
        }
    }
}