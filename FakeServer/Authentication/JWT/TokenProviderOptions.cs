using FakeServer.Common;
using Microsoft.IdentityModel.Tokens;
using System;

namespace FakeServer.Authentication.Jwt
{
    public class TokenProviderOptions
    {
        public string Path { get; set; } = $"/{Config.TokenRoute}";

        public string LogoutPath { get; set; } = $"/{Config.TokenLogoutRoute}";

        public string Issuer { get; set; }

        public string Audience { get; set; }

        public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(5);

        public SigningCredentials SigningCredentials { get; set; }
    }
}