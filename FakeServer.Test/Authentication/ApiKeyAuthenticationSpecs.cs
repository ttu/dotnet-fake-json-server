using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace FakeServer.Test.Authentication
{
    [Collection("Integration collection")]
    [Trait("category", "integration")]
    [Trait("category", "authentication")]
    public class ApiKeyAuthenticationSpecs : IDisposable
    {
        private readonly IntegrationFixture _fixture;

        public ApiKeyAuthenticationSpecs(IntegrationFixture fixture)
        {
            _fixture = fixture;
            _fixture.StartServer(authenticationType: "apikey");
        }

        public void Dispose()
        {
            _fixture.Stop();
        }

        [Fact]
        public async Task GetUsers_Unauthorized()
        {
            var result = await _fixture.Client.GetAsync("api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task GetUsers_Authorized()
        {
            _fixture.Client.DefaultRequestHeaders.Add("X-API-KEY", "correct-api-key");

            var result = await _fixture.Client.GetAsync("api/users");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task GetUsers_Wrong_Key_Unauthorized()
        {
            _fixture.Client.DefaultRequestHeaders.Add("X-API-KEY", "wrong-api-key");

            var result = await _fixture.Client.GetAsync("api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }
    }
}