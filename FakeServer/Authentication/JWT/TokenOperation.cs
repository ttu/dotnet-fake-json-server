using System.Collections.Generic;
using FakeServer.Common;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FakeServer.Authentication.Jwt
{
    internal class TokenOperation : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var logoutItem = new OpenApiPathItem();
            logoutItem.AddOperation(OperationType.Post, new OpenApiOperation
            {
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Authentication" } },
                Parameters = new List<OpenApiParameter>
                    {
                        new OpenApiParameter
                        {
                            //Type = "string",
                            Name = "Authorization",
                            Required = false,
                            In = ParameterLocation.Header
                        }
                    }
            });

            swaggerDoc.Paths.Add($"/{Config.TokenLogoutRoute}", logoutItem);
        }
    }
}
