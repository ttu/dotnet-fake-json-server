using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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

        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services)
        {
            services.AddSingleton<TokenBlacklistService>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Audience = _tokenValidationParameters.ValidAudience;
                    options.ClaimsIssuer = _tokenValidationParameters.ValidIssuer;
                    options.TokenValidationParameters = _tokenValidationParameters;
                    options.Events = new JwtBearerEvents()
                    {
                        OnTokenValidated = (context) =>
                        {
                            var header = context.Request.Headers["Authorization"];

                            var blacklist = context.HttpContext.RequestServices.GetService<TokenBlacklistService>();
                            if (blacklist.IsBlacklisted(header.ToString()))
                            {
                                context.Response.StatusCode = 401;
                                context.Fail(new Exception("Authorization token blacklisted"));
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        public static IOptions<TokenProviderOptions> GetOptions()
        {
            var options = new TokenProviderOptions
            {
                Audience = _tokenValidationParameters.ValidAudience,
                Issuer = _tokenValidationParameters.ValidIssuer,
                SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
            };

            var opts = Options.Create(options);

            return opts;
        }

        public static void UseTokenProviderMiddleware(this IApplicationBuilder app)
        {
            // Add JWT generation endpoint
            var opts = GetOptions();
            app.UseMiddleware<TokenProviderMiddleware>(opts);
            app.UseMiddleware<TokenLogoutMiddleware>(opts);
        }
    }
}