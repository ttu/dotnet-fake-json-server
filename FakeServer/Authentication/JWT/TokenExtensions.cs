using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FakeServer.Authentication.Jwt;

public static class TokenExtensions
{
    public static OpenApiSecurityScheme GetTokenSecurityDefinition(this SwaggerGenOptions s, string tokenPath)
    {
        return new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            Flows = new OpenApiOAuthFlows
            {
                ClientCredentials = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri(tokenPath, UriKind.Relative),
                    Scopes = new Dictionary<string, string>()
                }
            }
        };
    }

    public static OpenApiSecurityRequirement GetTokenSecurityRequirement(this SwaggerGenOptions s)
    {
        var securityRequirement = new OpenApiSecurityRequirement();
        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = JwtBearerDefaults.AuthenticationScheme
            }
        };

        securityRequirement.Add(scheme, new List<string>());

        return securityRequirement;
    }
}