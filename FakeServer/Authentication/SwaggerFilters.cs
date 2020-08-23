using FakeServer.Common;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace FakeServer.Authentication
{
    internal class AddAuthorizationHeaderParameterOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Description = "Authorization header. Usage e.g.: Bearer [access_token]",
                Required = false,
                //Type = "string"
            });
        }
    }

    internal class AuthTokenOperation : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var tokenItem =  new OpenApiPathItem();
            tokenItem.AddOperation(OperationType.Post, new OpenApiOperation
            {
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Authentication" } },
                //Consumes = new List<string> { "application/x-www-form-urlencoded" },
                Parameters = new List<OpenApiParameter>
                    {
                        new OpenApiParameter
                        {
                            //Type = "string",
                            Name = "username",
                            Required = false,
                            //In = "formData"
                        },
                        new OpenApiParameter
                        {
                            //Type = "string",
                            Name = "password",
                            Required = false,
                            //In = "formData"
                        }
                    }
            });

            swaggerDoc.Paths.Add($"/{Config.TokenRoute}", tokenItem);

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

            //swaggerDoc.Paths.Add($"/{Config.TokenLogoutRoute}", new PathItem
            //{
            //    Post = new Operation
            //    {
            //        Tags = new List<string> { "Authentication" },
            //        Parameters = new List<IParameter>
            //        {
            //            new NonBodyParameter
            //            {
            //                Type = "string",
            //                Name = "Authorization",
            //                Required = false,
            //                In = "header"
            //            }
            //        }
            //    }
            //});
        }
    }
}