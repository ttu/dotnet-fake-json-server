using Microsoft.OpenApi.Models;
using FakeServer.Authentication.Basic;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace FakeServer.Authentication
{
    public static class SwaggerGenOptionsExtensions
    {
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
}