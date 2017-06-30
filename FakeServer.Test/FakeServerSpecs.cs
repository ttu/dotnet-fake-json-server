using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;
using Xunit;

namespace FakeServer.Test
{
    // Tests in the same collection are not run in parallel
    // After each test data should be in the same state as in the beginning of the test or future tests might fail
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
                Assert.True(collections.Count() > 0);
            }
        }

        [Fact]
        public async Task GetUsers()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users");
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
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?skip=1&take=2");
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
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users/1");
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
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users/100000");
                Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
            }
        }

        [Fact]
        public async Task GetItem_QueryParams()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?name=Phil&age=25&take=5");
                var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Phil", allUsers[0]["name"].Value<string>());
                Assert.Equal("Box Company", allUsers[0]["work"]["name"].Value<string>());
            }
        }

        [Fact]
        public async Task GetItem_Nested()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/families/1/address/country");
                var country = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Brazil", country["name"].Value<string>());
            }
        }

        [Fact]
        public async Task GetItem_Nested_MultipleId()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/families/18/parents/1/work");
                var workplace = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal("EVIDENDS", workplace["companyName"].Value<string>());
            }
        }

        [Fact]
        public async Task PatchItem()
        {
            using (var client = new HttpClient())
            {
                // Original { "id": 1, "name": "James", "age": 40, "location": "NY", "work": { "name": "ACME", "location": "NY" } },
                var patchData = new { name = "Albert", age = 12, work = new { name = "EMACS" } };

                var content = new StringContent(JsonConvert.SerializeObject(patchData), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_fixture.BaseUrl}/api/users/1") { Content = content };
                var result = await client.SendAsync(request);
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/users/1"); ;
                result.EnsureSuccessStatusCode();
                var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal(patchData.name, item["name"].Value<string>());
                Assert.Equal(patchData.age, item["age"].Value<int>());
                Assert.Equal("NY", item["location"].Value<string>());
                Assert.Equal("EMACS", item["work"]["name"].Value<string>());
                Assert.Equal("NY", item["work"]["location"].Value<string>());

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/users");
                result.EnsureSuccessStatusCode();
                var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(4, items.Count());

                content = new StringContent(JsonConvert.SerializeObject(new { name = "James", age = 40, work = new { name = "ACME" } }), Encoding.UTF8, "application/json");
                request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_fixture.BaseUrl}/api/users/1") { Content = content };
                result = await client.SendAsync(request);
                result.EnsureSuccessStatusCode();
            }
        }

        [Fact]
        public async Task PostAndDeleteItem_ExistingCollection()
        {
            using (var client = new HttpClient())
            {
                // Try with "wrong" id
                var newUser = new { id = 8, name = "Newton" };

                var content = new StringContent(JsonConvert.SerializeObject(newUser), Encoding.UTF8, "application/json");
                var result = await client.PostAsync($"{_fixture.BaseUrl}/api/users", content);
                result.EnsureSuccessStatusCode();
                var postResponse = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal($"http://localhost:5001/api/users/{postResponse["id"]}", result.Headers.Location.OriginalString);

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/users/{postResponse["id"]}");
                result.EnsureSuccessStatusCode();
                var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal(newUser.name, item["name"].Value<string>());

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/users");
                result.EnsureSuccessStatusCode();
                var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(5, items.Count());

                result = await client.DeleteAsync($"{_fixture.BaseUrl}/api/users/{postResponse["id"]}");
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/users");
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
                var originalAmount = collections.Count();

                var content = new StringContent(JsonConvert.SerializeObject(newUser), Encoding.UTF8, "application/json");
                result = await client.PostAsync($"{_fixture.BaseUrl}/api/hello", content);
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync($"{_fixture.BaseUrl}/api");
                result.EnsureSuccessStatusCode();
                collections = JsonConvert.DeserializeObject<IEnumerable<string>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(originalAmount + 1, collections.Count());

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
                Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            }
        }

        [Fact]
        public async Task WebSockets_CalledTwice()
        {
            var are = new AutoResetEvent(false);

            var webSoketMessages = new List<dynamic>();

            var websocket = new WebSocket($"ws://localhost:{_fixture.Port}/ws");

            websocket.MessageReceived += (s, e) =>
            {
                webSoketMessages.Add(JsonConvert.DeserializeObject<dynamic>(e.Message));
                are.Set();
            };

            websocket.Opened += (s, e) =>
            {
                are.Set();
            };

            websocket.Open();

            are.WaitOne();

            using (var client = new HttpClient())
            {
                var patchData = new { name = "Albert", age = 12, work = new { name = "EMACS" } };

                var content = new StringContent(JsonConvert.SerializeObject(patchData), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_fixture.BaseUrl}/api/users/1") { Content = content };
                var result = await client.SendAsync(request);
                result.EnsureSuccessStatusCode();

                are.WaitOne();

                content = new StringContent(JsonConvert.SerializeObject(new { name = "James", age = 40, work = new { name = "ACME" } }), Encoding.UTF8, "application/json");
                request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_fixture.BaseUrl}/api/users/1") { Content = content };
                result = await client.SendAsync(request);
                result.EnsureSuccessStatusCode();

                are.WaitOne();
            }

            Assert.Equal(2, webSoketMessages.Count);
            Assert.Equal("PATCH", webSoketMessages[0].method.ToString());
            Assert.Equal("/api/users/1", webSoketMessages[0].path.ToString());
        }

        [Fact]
        public async Task Async_PostPutPathDelete()
        {
            async Task<HttpResponseMessage> GetWhenStatusNotOk(System.Uri queueUrl)
            {
                var handler = new HttpClientHandler { AllowAutoRedirect = false };

                using (var c = new HttpClient(handler))
                {
                    while (true)
                    {
                        var response = await c.GetAsync(queueUrl);

                        if (response.StatusCode != HttpStatusCode.OK)
                            return response;

                        Thread.Sleep(100);
                    }
                }
            }

            using (var client = new HttpClient())
            {
                var newBook = new { title = "Adventures of Robin Hood" };

                // POST

                var content = new StringContent(JsonConvert.SerializeObject(newBook), Encoding.UTF8, "application/json");
                var result = await client.PostAsync($"{_fixture.BaseUrl}/async/book", content);
                result.EnsureSuccessStatusCode();

                var queueUrl = result.Headers.Location;

                result = await client.GetAsync($"{queueUrl}NOTFOUND");
                Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);

                HttpResponseMessage resultForAction = await GetWhenStatusNotOk(queueUrl);

                Assert.Equal(HttpStatusCode.SeeOther, resultForAction.StatusCode);
                Assert.Equal("http://localhost:5001/api/book/0", resultForAction.Headers.Location.ToString());

                result = await client.GetAsync(resultForAction.Headers.Location);
                result.EnsureSuccessStatusCode();
                var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal(newBook.title, item["title"].Value<string>());

                // PUT

                var updateBook = new { title = "Adventures of Sherlock Holmes" };

                content = new StringContent(JsonConvert.SerializeObject(updateBook), Encoding.UTF8, "application/json");
                result = await client.PutAsync($"{_fixture.BaseUrl}/async/book/0", content);
                result.EnsureSuccessStatusCode();

                queueUrl = result.Headers.Location;

                resultForAction = await GetWhenStatusNotOk(queueUrl);
                Assert.Equal(HttpStatusCode.SeeOther, resultForAction.StatusCode);

                result = await client.GetAsync(resultForAction.Headers.Location);
                result.EnsureSuccessStatusCode();
                item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal(updateBook.title, item["title"].Value<string>());

                // PATCH

                var patchBook = new { author = "Edgar Allen Poe" };

                content = new StringContent(JsonConvert.SerializeObject(patchBook), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_fixture.BaseUrl}/async/book/0") { Content = content };
                result = await client.SendAsync(request);
                result.EnsureSuccessStatusCode();

                queueUrl = result.Headers.Location;

                resultForAction = await GetWhenStatusNotOk(queueUrl);
                Assert.Equal(HttpStatusCode.SeeOther, resultForAction.StatusCode);

                result = await client.GetAsync(resultForAction.Headers.Location);
                result.EnsureSuccessStatusCode();
                item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal(patchBook.author, item["author"].Value<string>());
                Assert.Equal(updateBook.title, item["title"].Value<string>());

                // DELETE

                result = await client.DeleteAsync($"{_fixture.BaseUrl}/async/book/0");
                result.EnsureSuccessStatusCode();

                queueUrl = result.Headers.Location;

                resultForAction = await GetWhenStatusNotOk(queueUrl);

                Assert.Equal(HttpStatusCode.SeeOther, resultForAction.StatusCode);
                Assert.Equal("http://localhost:5001/api/book/0", resultForAction.Headers.Location.ToString());

                // DELETE JOB

                result = await client.DeleteAsync(queueUrl);
                result.EnsureSuccessStatusCode();

                result = await client.GetAsync(queueUrl);
                Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            }
        }
    }
}