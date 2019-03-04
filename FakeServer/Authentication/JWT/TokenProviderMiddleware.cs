using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FakeServer.Authentication.Jwt
{
    public class TokenProviderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenProviderOptions _options;

        public TokenProviderMiddleware(
            RequestDelegate next,
            IOptions<TokenProviderOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            // If the request path doesn't match, skip
            if (!context.Request.Path.Equals(_options.Path, StringComparison.Ordinal))
            {
                await _next(context);
                return;
            }

            // Request must be POST with Content-Type: application/x-www-form-urlencoded or application/json
            if (!context.Request.Method.Equals("POST"))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Only POST method allowed.");
                return;
            }
            else if (!context.Request.HasFormContentType && !context.Request.ContentType.StartsWith("application/json"))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Only Content-Type: application/x-www-form-urlencoded or application/json are allowed.");
                return;
            }

            (string username, string password, bool isDataValid) = context.Request.HasFormContentType 
                                                                    ? GetFromFormData(context)
                                                                    : await GetFromJson(context);

            if (!isDataValid)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Authentication must be defined with username and password fields");
                return;
            }

            await GenerateToken(context, username, password);
        }

        private async Task<(string username, string password, bool validData)> GetFromJson(HttpContext context)
        {
            string body;

            using (var streamReader = new StreamReader(context.Request.Body))
            {
                body = await streamReader.ReadToEndAsync().ConfigureAwait(true);
            }

            var jsonBody = JObject.Parse(body);

            if (!jsonBody.ContainsKey("username") || !jsonBody.ContainsKey("password"))
                return (string.Empty, string.Empty, false);

            return (jsonBody["username"].Value<string>(), jsonBody["password"].Value<string>(), true);
        }

        private (string username, string password, bool validData) GetFromFormData(HttpContext context)
        {
            if (!context.Request.Form.ContainsKey("username") || !context.Request.Form.ContainsKey("password"))
                return (string.Empty, string.Empty, false);

            return (context.Request.Form["username"], context.Request.Form["password"], true);
        }

        private async Task GenerateToken(HttpContext context, string username, string password)
        {
            var authenticationSettings = context.RequestServices.GetService(typeof(IOptions<AuthenticationSettings>)) as IOptions<AuthenticationSettings>;

            var identity = await GetIdentity(username, password, authenticationSettings.Value);

            if (identity == null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid username or password.");
                return;
            }

            var now = DateTime.UtcNow;

            // Specifically add the jti (random nonce), iat (issued timestamp), and sub (subject/user) claims.
            // You can add other claims here, if you want:
            var claims = new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Create the JWT and write it to a string
            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: now.Add(_options.Expiration),
                signingCredentials: _options.SigningCredentials);

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new
            {
                access_token = encodedJwt,
                expires_in = (int)_options.Expiration.TotalSeconds
            };

            // Serialize and return the response
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response, new JsonSerializerSettings { Formatting = Formatting.Indented }));
        }

        private Task<ClaimsIdentity> GetIdentity(string username, string password, AuthenticationSettings authenticationSettings)
        {
            if (authenticationSettings.Users.Any(e => e.Username == username && e.Password == password))
            {
                return Task.FromResult(new ClaimsIdentity(new System.Security.Principal.GenericIdentity(username, "Token"), new Claim[] { }));
            }

            // Credentials are invalid, or account doesn't exist
            return Task.FromResult<ClaimsIdentity>(null);
        }
    }
}