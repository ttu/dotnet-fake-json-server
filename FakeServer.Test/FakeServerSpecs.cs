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
                Assert.Equal("Ok", status["status"].Value<string>());
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

                var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(4, items.Count());
            }
        }

        [Fact]
        public async Task GetUsers_SkipTake()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/user?skip=1&take=2");
                result.EnsureSuccessStatusCode();

                var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(2, items.Count());
                Assert.Equal("2", items.First()["id"]);
            }
        }

        [Fact]
        public async Task GetItem_WithId_Found()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/user/1");
                Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);

                var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal("James", item["name"].Value<string>());
            }
        }

        [Fact]
        public async Task GetItem_WithId_NotFound()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/user/100000");
                Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
            }
        }

        [Fact]
        public async Task GetItem_QueryParams()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/user?name=Phil&age=25&take=5");
                var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Phil", allUsers[0]["name"].Value<string>());
            }
        }

        [Fact]
        public async Task GetItem_Nested()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/family/1/address/country");
                var country = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Brazil", country["name"].Value<string>());
            }
        }

        [Fact]
        public async Task GetItem_Nested_MultipleId()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/family/18/parents/1/work");
                var workplace = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal("EVIDENDS", workplace["companyName"].Value<string>());
            }
        }

        [Fact]
        public async Task PatchItem()
        {
            using (var client = new HttpClient())
            {
                // Original { "id": 1, "name": "James", "age": 40, "location": "NY" },
                var patchData = new { name = "Albert", age = 12 };

                var content = new StringContent(JsonConvert.SerializeObject(patchData), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_fixture.BaseUrl}/api/user/1") { Content = content };
                var result = await client.SendAsync(request);
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/user/1"); ;
                result.EnsureSuccessStatusCode();
                var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal(patchData.name, item["name"].Value<string>());
                Assert.Equal(patchData.age, item["age"].Value<int>());
                Assert.Equal("NY", item["location"].Value<string>());

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/user");
                result.EnsureSuccessStatusCode();
                var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(4, items.Count());

                content = new StringContent(JsonConvert.SerializeObject(new { name = "James", age = 40 }), Encoding.UTF8, "application/json");
                request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_fixture.BaseUrl}/api/user/1") { Content = content };
                result = await client.SendAsync(request);
                result.EnsureSuccessStatusCode();
            }
        }

        [Fact]
        public async Task PostAndDeleteItem_ExistingCollection()
        {
            using (var client = new HttpClient())
            {
                var newUser = new { id = 5, name = "Newton" };

                var content = new StringContent(JsonConvert.SerializeObject(newUser), Encoding.UTF8, "application/json");
                var result = await client.PostAsync($"{_fixture.BaseUrl}/api/user", content);
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/user/5"); ;
                result.EnsureSuccessStatusCode();
                var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal(newUser.name, item["name"].Value<string>());

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/user");
                result.EnsureSuccessStatusCode();
                var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(5, items.Count());

                result = await client.DeleteAsync($"{_fixture.BaseUrl}/api/user/5");
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/user");
                result.EnsureSuccessStatusCode();
                items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(4, items.Count());
            }
        }

        [Fact]
        public async Task PostAndDeleteItem_NewCollection()
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

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/hello/0");
                result.EnsureSuccessStatusCode();
                var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal(newUser.name, item["name"].Value<string>());

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/hello");
                result.EnsureSuccessStatusCode();
                var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(1, items.Count());

                result = await client.DeleteAsync($"{_fixture.BaseUrl}/api/hello/0");
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/hello");
                result.EnsureSuccessStatusCode();
                items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(0, items.Count());
            }
        }
    }
}