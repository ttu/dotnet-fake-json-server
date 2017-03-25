using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FakeServer.Test
{
    // Tests in the same collection are not run in parallel
    [Collection("Integration collection")]
    public class FakeServerSpecs
    {
        private IntegrationFixture _fixture;

        public FakeServerSpecs(IntegrationFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Status()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/status");
                var status = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Ok", status["Status"].Value<string>());
            }
        }

        [Fact]
        public async Task GetCollections()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/");
                result.EnsureSuccessStatusCode();

                var collections = JsonConvert.DeserializeObject<IEnumerable<string>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(3, collections.Count());
            }
        }

        [Fact]
        public async Task GetUsers()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/user");
                result.EnsureSuccessStatusCode();

                var entities = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(4, entities.Count());
            }
        }

        [Fact]
        public async Task GetEntity()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/user/1");
                result.EnsureSuccessStatusCode();

                var enityt = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal("James", enityt["name"].Value<string>());
            }
        }

        [Fact]
        public async Task GetEntity_QueryParams()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/user?name=Phil&age=25");
                var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Phil", allUsers[0]["name"].Value<string>());
            }
        }

        [Fact]
        public async Task PostAndDeleteEntity_ExistingCollection()
        {
            using (var client = new HttpClient())
            {
                var newUser = new { id = 5, name = "Newton" };

                var content = new StringContent(JsonConvert.SerializeObject(newUser), Encoding.UTF8, "application/json");
                var result = await client.PostAsync($"{_fixture.BaseUrl}/api/user", content);
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/user/5"); ;
                result.EnsureSuccessStatusCode();
                var enityt = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal(newUser.name, enityt["name"].Value<string>());

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/user");
                result.EnsureSuccessStatusCode();
                var entities = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(5, entities.Count());

                result = await client.DeleteAsync($"{_fixture.BaseUrl}/api/user/5");
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/user");
                result.EnsureSuccessStatusCode();
                entities = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(4, entities.Count());
            }
        }

        [Fact]
        public async Task PostAndDeleteEntity_NewCollection()
        {
            using (var client = new HttpClient())
            {
                var newUser = new { id = 1, name = "Newton" };

                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/");
                result.EnsureSuccessStatusCode();
                var collections = JsonConvert.DeserializeObject<IEnumerable<string>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(3, collections.Count());

                var content = new StringContent(JsonConvert.SerializeObject(newUser), Encoding.UTF8, "application/json");
                result = await client.PostAsync($"{_fixture.BaseUrl}/api/hello", content);
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync($"{_fixture.BaseUrl}/api");
                result.EnsureSuccessStatusCode();
                collections = JsonConvert.DeserializeObject<IEnumerable<string>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(4, collections.Count());

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/hello/1");
                result.EnsureSuccessStatusCode();
                var enityt = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal(newUser.name, enityt["name"].Value<string>());

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/hello");
                result.EnsureSuccessStatusCode();
                var entites = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(1, entites.Count());

                result = await client.DeleteAsync($"{_fixture.BaseUrl}/api/hello/1");
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/hello");
                result.EnsureSuccessStatusCode();
                entites = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(0, entites.Count());
            }
        }
    }
}