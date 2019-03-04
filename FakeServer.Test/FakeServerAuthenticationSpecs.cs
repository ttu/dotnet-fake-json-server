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
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users");
                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetUsers_Authorized_Logout(bool userFormContent)
        {
            using (var client = new HttpClient())
            {
                var token = userFormContent ? await GetTokenFormContent() : await GetTokenJsonContent();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);

                var logoutResult = await client.PostAsync($"{_fixture.BaseUrl}/logout", null);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);

                var afterLogoutResult = await client.GetAsync($"{_fixture.BaseUrl}/api/users");
                Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutResult.StatusCode);
            }
        }

        private async Task<string> GetTokenFormContent()
        {
            using (var client = new HttpClient())
            {
                var items = new[]
                {
                    new KeyValuePair<string,string>("username","admin"),
                    new KeyValuePair<string,string>("password","root")
                };

                var content = new FormUrlEncodedContent(items);

                var result = await client.PostAsync($"{_fixture.BaseUrl}/token", content);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);

                var response = await result.Content.ReadAsStringAsync();
                return JObject.Parse(response)["access_token"].Value<string>();
            }
        }

        private async Task<string> GetTokenJsonContent()
        {
            using (var client = new HttpClient())
            {
                var userData = new { username = "admin", password = "root" };

                var content = new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json");

                var result = await client.PostAsync($"{_fixture.BaseUrl}/token", content);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);

                var response = await result.Content.ReadAsStringAsync();
                return JObject.Parse(response)["access_token"].Value<string>();
            }
        }

        [Fact]
        public async Task GetToken_InvalidMethod()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/token");
                Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            }
        }

        [Fact]
        public async Task GetToken_InvalidFormData()
        {
            using (var client = new HttpClient())
            {
                var items = new[]
                {
                    new KeyValuePair<string,string>("uname","admin"),
                    new KeyValuePair<string,string>("pwd","root")
                };

                var content = new FormUrlEncodedContent(items);

                var result = await client.PostAsync($"{_fixture.BaseUrl}/token", content);
                Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            }
        }

        [Fact]
        public async Task GetToken_InvalidJsonData()
        {
            using (var client = new HttpClient())
            {
                var userData = new { username = "admin", pwd = "root" };

                var content = new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json");

                var result = await client.PostAsync($"{_fixture.BaseUrl}/token", content);
                Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            }
        }
    }
}