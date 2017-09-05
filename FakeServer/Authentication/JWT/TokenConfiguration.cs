using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace FakeServer.Authentication.Jwt
{
    public static class TokenConfiguration
    {
        // secretKey contains a secret passphrase only your server knows
        private static string _secretKey = "mysupersecret_secretkey!123";

        private static SymmetricSecurityKey _signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secretKey));

        private static TokenValidationParameters _tokenValidationParameters = new TokenValidationParameters
        {
            // The signing key must match!
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,

            // Validate the JWT Issuer (iss) claim
            ValidateIssuer = true,
            ValidIssuer = "ExampleIssuer",

            // Validate the JWT Audience (aud) claim
            ValidateAudience = true,
            ValidAudience = "ExampleAudience",

            // Validate the token expiry
            ValidateLifetime = true,

            // If you want to allow a certain amount of clock drift, set that here:
            ClockSkew = TimeSpan.Zero
        };

        public static void Configure(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                                {
                                    options.Audience = _tokenValidationParameters.ValidAudience;
                                    options.ClaimsIssuer = _tokenValidationParameters.ValidIssuer;
                                    options.TokenValidationParameters = _tokenValidationParameters;
                                });
        }

        public static void UseTokenProviderMiddleware(IApplicationBuilder app)
        {
            // Add JWT generation endpoint
            var options = new TokenProviderOptions
            {
                Audience = _tokenValidationParameters.ValidAudience,
                Issuer = _tokenValidationParameters.ValidIssuer,
                SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
            };

            app.UseMiddleware<TokenProviderMiddleware>(Options.Create(options));
        }
    }
}