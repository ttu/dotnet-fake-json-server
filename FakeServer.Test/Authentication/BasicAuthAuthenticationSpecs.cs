using System;
using System.Net.Http.Headers;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace FakeServer.Test.Authentication
{
    [Collection("Integration collection")]
    [Trait("category", "integration")]
    [Trait("category", "authentication")]
    public class BasicAuthAuthenticationSpecs : IDisposable
    {
        private readonly IntegrationFixture _fixture;

        public BasicAuthAuthenticationSpecs(IntegrationFixture fixture)
        {
            _fixture = fixture;
            _fixture.StartServer(authenticationType: "basic");
        }

        public void Dispose()
        {
            _fixture.Stop();
        }

        [Fact]
        public async Task GetUsers_No_Header_Unauthorized()
        {
            var result = await _fixture.Client.GetAsync("api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task GetUsers_Authorized()
        {
            _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "YWRtaW46cm9vdA==");

            var result = await _fixture.Client.GetAsync("api/users");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Theory]
        [InlineData("abbaacdc1234")]
        [InlineData("")]
        public async Task GetUsers_Wrong_Token_Unauthorized(string token)
        {
            _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);


            var result = await _fixture.Client.GetAsync("api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }
    }
}