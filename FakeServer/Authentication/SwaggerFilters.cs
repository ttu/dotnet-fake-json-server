using FakeServer.Common;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace FakeServer.Authentication
{
    internal class AddAuthorizationHeaderParameterOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<IParameter>();

            operation.Parameters.Add(new NonBodyParameter
            {
                Name = "Authorization",
                In = "header",
                Description = "Authorization header. Usage e.g.: Bearer [access_token]",
                Required = false,
                Type = "string"
            });
        }
    }

    internal class AuthTokenOperation : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Paths.Add($"/{Config.TokenRoute}", new PathItem
            {
                Post = new Operation
                {
                    Tags = new List<string> { "Authentication" },
                    Consumes = new List<string>
                    {
                        "application/x-www-form-urlencoded"
                    },
                    Parameters = new List<IParameter>
                    {
                        new NonBodyParameter
                        {
                            Type = "string",
                            Name = "username",
                            Required = false,
                            In = "formData"
                        },
                        new NonBodyParameter
                        {
                            Type = "string",
                            Name = "password",
                            Required = false,
                            In = "formData"
                        }
                    }
                }
            });
        }
    }
}