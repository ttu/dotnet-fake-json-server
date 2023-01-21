using FakeServer.Authentication.ApiKey;
using FakeServer.Authentication.Basic;
using FakeServer.Authentication.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FakeServer.Authentication;

public static class SwaggerConfiguration
{
    public static  void AddAuthenticationConfig(this SwaggerGenOptions c, AuthenticationType authenticationType)
    {
        switch (authenticationType)
        {
            case AuthenticationType.JwtBearer:
                var tokenPath = TokenConfiguration.GetOptions().Value.Path;
                c.DocumentFilter<TokenOperation>();
                c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, c.GetTokenSecurityDefinition(tokenPath));
                c.AddSecurityRequirement(c.GetTokenSecurityRequirement());
                break;

            case AuthenticationType.Basic:
                c.AddSecurityDefinition(BasicAuthenticationDefaults.AuthenticationScheme, c.GetBasicSecurityDefinition());
                c.AddSecurityRequirement(c.GetBasicSecurityRequirement());
                break;

            case AuthenticationType.ApiKey:
                c.AddSecurityDefinition(ApiKeyAuthenticationDefaults.AuthenticationScheme, c.GetApiKeySecurityDefinition());
                c.AddSecurityRequirement(c.GetApiKeySecurityRequirement());
                break;

            case AuthenticationType.AllowAll:
                break;

            default: throw new ArgumentException(nameof(authenticationType) + " is not a valid authentication type");
        }
    }
}