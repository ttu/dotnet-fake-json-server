using System;
using FakeServer.Authentication.Basic;
using FakeServer.Authentication.Custom;
using FakeServer.Authentication.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FakeServer.Authentication
{
    static class AuthenticationConfiguration
    {
        public static AuthenticationType ReadType(IConfiguration configuration)
        {
            if (configuration.GetValue<bool>("Authentication:Enabled"))
            {
                return configuration["Authentication:AuthenticationType"] == "token"
                    ? AuthenticationType.JwtBearer
                    : AuthenticationType.Basic;
            }
            return AuthenticationType.AllowAll;
        }

        public static IServiceCollection AddAuthentication(this IServiceCollection services, AuthenticationType type)
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
                default: throw new ArgumentException(nameof(type) + " is not a valid authentication type");
            }

            return services;
        }
    }
}