using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FakeServer.Test
{
    [Collection("Integration collection")]
    [Trait("category", "integration")]
    [Trait("category", "authentication")]
    public class FakeServerApiKeyAuthenticationSpecs : IDisposable
    {
        private readonly IntegrationFixture _fixture;

        public FakeServerApiKeyAuthenticationSpecs(IntegrationFixture fixture)
        {
            _fixture = fixture;
            _fixture.StartServer(authenticationType: "ApiKey");
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
            _fixture.Client.DefaultRequestHeaders.Add("X-API-Key", "correct-api-key");
                
            var result = await _fixture.Client.GetAsync("api/users");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }
        
        [Fact]
        public async Task GetUsers_Wrong_Key_Unauthorized()
        {
            _fixture.Client.DefaultRequestHeaders.Add("X-API-Key", "wrong-api-key");
                
            var result = await _fixture.Client.GetAsync("api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }
    }
}