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
    public class FakeServerAuthenticationSpecs : IDisposable
    {
        private readonly IntegrationFixture _fixture;

        public FakeServerAuthenticationSpecs(IntegrationFixture fixture)
        {
            _fixture = fixture;
            _fixture.StartServer(authenticationType: "token");
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

        public enum TokenType
        {
            Form,
            Json,
            ClientCredentials
        }

        [Theory]
        [InlineData(TokenType.Form)]
        [InlineData(TokenType.Json)]
        [InlineData(TokenType.ClientCredentials)]
        public async Task GetUsers_Authorized_Logout(TokenType tokenType)
        {
            var token = await GetToken(tokenType);

            _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var result = await _fixture.Client.GetAsync("api/users");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var logoutResult = await _fixture.Client.PostAsync("logout", null);
            Assert.Equal(HttpStatusCode.OK, logoutResult.StatusCode);

            var afterLogoutResult = await _fixture.Client.GetAsync("api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutResult.StatusCode);
        }

        private async Task<string> GetToken(TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.Form: return await GetTokenFormContent();
                case TokenType.Json: return await GetTokenJsonContent();
                case TokenType.ClientCredentials: return await GetTokenClientCredentials();
                default: throw new NotImplementedException(tokenType.ToString());
            }
        }

        private async Task<string> GetTokenFormContent()
        {
            var items = new[]
            {
                new KeyValuePair<string,string>("username","admin"),
                new KeyValuePair<string,string>("password","root")
            };

            var content = new FormUrlEncodedContent(items);

            var result = await _fixture.Client.PostAsync("token", content);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var response = await result.Content.ReadAsStringAsync();
            return JObject.Parse(response)["access_token"].Value<string>();
        }

        private async Task<string> GetTokenJsonContent()
        {
            var userData = new { username = "admin", password = "root" };

            var content = new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json");

            var result = await _fixture.Client.PostAsync("token", content);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var response = await result.Content.ReadAsStringAsync();
            return JObject.Parse(response)["access_token"].Value<string>();
        }

        private async Task<string> GetTokenClientCredentials()
        {
            var postData = new[]
            {
                new KeyValuePair<string,string>("grant_type", "client_credentials"),
                new KeyValuePair<string,string>("client_id", "admin"),
                new KeyValuePair<string,string>("client_secret", "root")
            };

            var content = new FormUrlEncodedContent(postData);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var result = await _fixture.Client.PostAsync("token", content);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var response = await result.Content.ReadAsStringAsync();
            return JObject.Parse(response)["access_token"].Value<string>();
        }

        [Fact]
        public async Task GetToken_InvalidMethod()
        {
            var result = await _fixture.Client.GetAsync("token");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task GetToken_InvalidFormData()
        {
            var items = new[]
            {
                new KeyValuePair<string,string>("uname","admin"),
                new KeyValuePair<string,string>("pwd","root")
            };

            var content = new FormUrlEncodedContent(items);

            var result = await _fixture.Client.PostAsync("token", content);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task GetToken_InvalidJsonData()
        {
            var userData = new { username = "admin", pwd = "root" };

            var content = new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json");

            var result = await _fixture.Client.PostAsync("token", content);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }
}