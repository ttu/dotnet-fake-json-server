using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
    public class FakeServerSpecs : IDisposable
    {
        private readonly IntegrationFixture _fixture;

        public FakeServerSpecs(IntegrationFixture fixture)
        {
            _fixture = fixture;
            _fixture.StartServer();
        }

        public void Dispose()
        {
            _fixture.Stop();
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

        [Theory]
        [InlineData("sort=location")]
        [InlineData("sort=-location")]
        public async Task GetUsers_SortByLocationDescending(string queryParams)
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?{queryParams}");
                result.EnsureSuccessStatusCode();

                var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(4, items.Count());
                Assert.Equal("SF", items.ToList()[0]["location"]);
                Assert.Equal("SF", items.ToList()[1]["location"]);
                Assert.Equal("NY", items.ToList()[2]["location"]);
            }
        }

        [Theory]
        [InlineData("sort=+location")]
        [InlineData("sort= location")]
        public async Task GetUsers_SortByLocationAscending(string queryParams)
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?{queryParams}");
                result.EnsureSuccessStatusCode();

                var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(4, items.Count());
                Assert.Equal("London", items.ToList()[0]["location"]);
                Assert.Equal("NY", items.ToList()[1]["location"]);
                Assert.Equal("SF", items.ToList()[2]["location"]);
            }
        }

        [Theory]
        [InlineData("sort=location,age")]
        [InlineData("sort=location,-age")]
        public async Task GetUsers_SortByLocationAndAgeDescending(string queryParams)
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?{queryParams}");
                result.EnsureSuccessStatusCode();

                var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(4, items.Count());
                Assert.Equal("SF", items.ToList()[0]["location"]);
                Assert.Equal("SF", items.ToList()[1]["location"]);
                Assert.Equal("NY", items.ToList()[2]["location"]);

                Assert.Equal("52", items.ToList()[0]["age"]);
                Assert.Equal("30", items.ToList()[1]["age"]);
            }
        }

        [Theory]
        [InlineData("sort=location,+age")]
        [InlineData("sort=location, age")]
        public async Task GetUsers_SortByLocationDefaultAndAgeAscending(string queryParams)
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?{queryParams}");
                result.EnsureSuccessStatusCode();

                var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
                Assert.Equal(4, items.Count());
                Assert.Equal("SF", items.ToList()[0]["location"]);
                Assert.Equal("SF", items.ToList()[1]["location"]);
                Assert.Equal("NY", items.ToList()[2]["location"]);

                Assert.Equal("30", items.ToList()[0]["age"]);
                Assert.Equal("52", items.ToList()[1]["age"]);
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
        public async Task GetItem_PropertyNotFound()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?notfound=AA");
                var foundUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Empty(foundUsers);
            }
        }

        [Fact]
        public async Task GetItem_QueryFilters_NotEqual()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?age_ne=25");
                var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal(3, allUsers.Count());
            }
        }

        [Fact]
        public async Task GetItem_QueryFilters_LessThan()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?age_lt=30");
                var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Single(allUsers);
            }
        }

        [Fact]
        public async Task GetItem_QueryFilters_LessThanEquals()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?age_lte=30");
                var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal(2, allUsers.Count());
            }
        }

        [Fact]
        public async Task GetItem_QueryFilters_GreaterThan()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?work.rating_gt=1.6");
                var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal(2, allUsers.Count());
            }
        }

        [Fact]
        public async Task GetItem_QueryFilters_GreaterThanEquals()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?work.rating_gte=1.6");
                var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal(3, allUsers.Count());
            }
        }

        [Fact]
        public async Task GetItem_QueryFilters_DateTime()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/families?bankAccount.opened_lt=1/1/2015");
                var allFamilies = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal(8, allFamilies.Count());
            }
        }

        [Fact]
        public async Task GetItem_QueryFilters_Bool()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/families?bankAccount.isActive=true");
                var allFamilies = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal(9, allFamilies.Count());
            }
        }

        [Fact]
        public async Task GetItem_Fields()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?fields=age,name");
                var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal(2, allUsers[0].Count());
                Assert.NotNull(allUsers[0]["age"]);
                Assert.NotNull(allUsers[0]["name"]);
                Assert.Null(allUsers[0]["id"]);
            }
        }

        [Fact]
        public async Task GetItem_TextSearch_BadRequest()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/families?q=some&k=fail");
                Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            }
        }

        [Fact]
        public async Task GetItem_TextSearch()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/families?q=Hillsboro");
                var allFamilies = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal(3, allFamilies.Count());
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
                Assert.Single(items);

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
        public async Task Async_PostPutPatchDelete()
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

                        await Task.Delay(100);
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

        [Fact]
        public async Task GetOptions()
        {
            using (var client = new HttpClient())
            {
                /*
                NOTE: HttpClient adds Allow as an invalid header so it is not in the header collection but it is visible with ToString()
                result.ToString()
                StatusCode: 200, ReasonPhrase: 'OK', Version: 1.1, Content: System.Net.Http.StreamContent, Headers:
                {
                  Date: Wed, 26 Jul 2017 06:18:14 GMT
                  Transfer-Encoding: chunked
                  Server: Kestrel
                  Allow: GET
                  Allow: POST
                  Allow: OPTIONS
                }
                */

                string GetAllow(HttpResponseMessage message)
                {
                    var items = message.ToString().Replace("\r\n", " ").Split(' ');

                    var words = items.Select((val, idx) => val == "Allow:" ? items[idx + 1] : null)
                                     .Where(e => !string.IsNullOrEmpty(e))
                                     .Select(e => e.Trim());

                    return string.Join(", ", words);
                }

                var request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"{_fixture.BaseUrl}/api");
                var result = await client.SendAsync(request);
                Assert.Equal("GET, HEAD, POST, OPTIONS", GetAllow(result));

                request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"{_fixture.BaseUrl}/api/users");
                result = await client.SendAsync(request);
                Assert.Equal("GET, HEAD, POST, OPTIONS", GetAllow(result));

                request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"{_fixture.BaseUrl}/api/users/22");
                result = await client.SendAsync(request);
                Assert.Equal("GET, HEAD, POST, PUT, PATCH, DELETE, OPTIONS", GetAllow(result));

                request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"{_fixture.BaseUrl}/async/users");
                result = await client.SendAsync(request);
                Assert.Equal("POST, OPTIONS", GetAllow(result));

                request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"{_fixture.BaseUrl}/async/users/22");
                result = await client.SendAsync(request);
                Assert.Equal("PUT, PATCH, DELETE, OPTIONS", GetAllow(result));

                request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"{_fixture.BaseUrl}/async/queue/22");
                result = await client.SendAsync(request);
                Assert.Equal("GET, DELETE, OPTIONS", GetAllow(result));
            }
        }

        [Fact]
        public async Task GetHead_Families_Count()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(new HttpMethod("HEAD"), $"{_fixture.BaseUrl}/api/families");
                var result = await client.SendAsync(request);
                result.EnsureSuccessStatusCode();

                var countHeader = result.Headers.GetValues("X-Total-Count").First();
                Assert.Equal("20", countHeader);

                var body = await result.Content.ReadAsStringAsync();
                Assert.Equal(string.Empty, body);
            }
        }

        [Fact]
        public async Task GetHead_NotFound()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(new HttpMethod("HEAD"), $"{_fixture.BaseUrl}/api/users/2000");
                var result = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);

                var body = await result.Content.ReadAsStringAsync();
                Assert.Equal(string.Empty, body);
            }
        }

        [Fact]
        public async Task GetHead_ETag()
        {
            using (var client = new HttpClient())
            {
                var requestHead = new HttpRequestMessage(new HttpMethod("HEAD"), $"{_fixture.BaseUrl}/api/families");
                var resultHead = await client.SendAsync(requestHead);
                resultHead.EnsureSuccessStatusCode();

                var requestGet = new HttpRequestMessage(new HttpMethod("GET"), $"{_fixture.BaseUrl}/api/families");
                var resultGet = await client.SendAsync(requestGet);
                resultGet.EnsureSuccessStatusCode();

                var etagHeaderHead = resultHead.Headers.GetValues("ETag").First();
                Assert.False(string.IsNullOrEmpty(etagHeaderHead));

                var etagHeaderGet = resultGet.Headers.GetValues("ETag").First();
                Assert.False(string.IsNullOrEmpty(etagHeaderGet));

                Assert.Equal(etagHeaderHead, etagHeaderGet);
            }
        }

        [Fact]
        public async Task GetPaginationHeaders()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/families?skip=7&take=4");
                result.EnsureSuccessStatusCode();

                var countHeader = result.Headers.GetValues("X-Total-Count").First();
                Assert.Equal("20", countHeader);

                var linksHeaders = result.Headers.GetValues("Link").First();
                Assert.Contains(@"<http://localhost:5001/api/families?skip=3&take=4>; rel=""prev""", linksHeaders);
                Assert.Contains(@"<http://localhost:5001/api/families?skip=11&take=4>; rel=""next""", linksHeaders);
                Assert.Contains(@"<http://localhost:5001/api/families?skip=0&take=4>; rel=""first""", linksHeaders);
                Assert.Contains(@"<http://localhost:5001/api/families?skip=16&take=4>; rel=""last""", linksHeaders);

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/families?skip=0&take=21");

                countHeader = result.Headers.GetValues("X-Total-Count").First();
                Assert.Equal("20", countHeader);

                linksHeaders = result.Headers.GetValues("Link").First();
                Assert.True(string.IsNullOrEmpty(linksHeaders));

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/families?skip=0&take=10");

                linksHeaders = result.Headers.GetValues("Link").First();
                Assert.DoesNotContain("prev", linksHeaders);
                Assert.DoesNotContain("first", linksHeaders);
                Assert.Contains("next", linksHeaders);
                Assert.Contains("last", linksHeaders);
            }
        }

        [Fact]
        public async Task GetPaginationHeaders_offset_limit()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/families?offset=2&limit=17");

                var countHeader = result.Headers.GetValues("X-Total-Count").First();
                Assert.Equal("20", countHeader);

                var linksHeaders = result.Headers.GetValues("Link").First();
                Assert.Contains(@"<http://localhost:5001/api/families?offset=0&limit=2>; rel=""prev""", linksHeaders);
                Assert.Contains(@"<http://localhost:5001/api/families?offset=19&limit=17>; rel=""next""", linksHeaders);
                Assert.Contains(@"<http://localhost:5001/api/families?offset=0&limit=17>; rel=""first""", linksHeaders);
                Assert.Contains(@"<http://localhost:5001/api/families?offset=3&limit=17>; rel=""last""", linksHeaders);
            }
        }

        [Fact]
        public async Task GetItem_ETag_Cached_NoHeader()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?name=Phil");
                var originalEtag = result.Headers.ETag.Tag;

                var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Phil", allUsers[0]["name"].Value<string>());
                Assert.Equal("Box Company", allUsers[0]["work"]["name"].Value<string>());

                var request = new HttpRequestMessage(new HttpMethod("GET"), $"{_fixture.BaseUrl}/api/users?name=Phil&age=25");

                result = await client.SendAsync(request);
                var newEtag = result.Headers.ETag.Tag;

                Assert.Equal(originalEtag, newEtag);

                allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Phil", allUsers[0]["name"].Value<string>());
                Assert.Equal("Box Company", allUsers[0]["work"]["name"].Value<string>());
            }
        }

        [Fact]
        public async Task GetItem_ETag_Cached_IfNoneMatch_Header()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users?name=Phil");
                var originalEtag = result.Headers.ETag.Tag;

                var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Phil", allUsers[0]["name"].Value<string>());
                Assert.Equal("Box Company", allUsers[0]["work"]["name"].Value<string>());

                var request = new HttpRequestMessage(new HttpMethod("GET"), $"{_fixture.BaseUrl}/api/users?name=Phil&age=25");
                request.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(originalEtag));

                result = await client.SendAsync(request);
                var newEtag = result.Headers.ETag.Tag;

                Assert.Equal(originalEtag, newEtag);
                Assert.Equal(HttpStatusCode.NotModified, result.StatusCode);

                var content = await result.Content.ReadAsStringAsync();
                Assert.Equal(string.Empty, content);
            }
        }

        [Fact]
        public async Task GetItem_ETag_Cached_Put()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/api/users/1");
                var originalEtag = result.Headers.ETag.Tag;

                var user = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal("James", user["name"].Value<string>());
                Assert.Equal("ACME", user["work"]["name"].Value<string>());

                user["work"]["name"] = "Other Company";
                var request = new HttpRequestMessage(new HttpMethod("PUT"), $"{_fixture.BaseUrl}/api/users/1");
                request.Content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(originalEtag));

                result = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/users/1");

                user = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal("James", user["name"].Value<string>());
                Assert.Equal("Other Company", user["work"]["name"].Value<string>());

                // Try to update with original Tag

                user["work"]["name"] = "No Company";
                var request2 = new HttpRequestMessage(new HttpMethod("PUT"), $"{_fixture.BaseUrl}/api/users/1");
                request2.Content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                request2.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(originalEtag));

                result = await client.SendAsync(request2);
                Assert.Equal(HttpStatusCode.PreconditionFailed, result.StatusCode);

                result = await client.GetAsync($"{_fixture.BaseUrl}/api/users/1");

                user = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
                Assert.Equal("James", user["name"].Value<string>());
                Assert.Equal("Other Company", user["work"]["name"].Value<string>());
            }
        }

        [Fact]
        public async Task PostGraphQL()
        {
            using (var client = new HttpClient())
            {
                var q = @"
                    query {
                          families {
                            familyName
                            children(age: 4) {
                              name
                              age
                            }
                          }
                    }";

                var content = new StringContent(q, Encoding.UTF8, "application/graphql");
                var result = await client.PostAsync($"{_fixture.BaseUrl}/graphql", content);

                Assert.Equal(HttpStatusCode.OK, result.StatusCode);

                var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

                Assert.NotNull(data["data"]);
                Assert.Null(data["errors"]);
            }
        }

        [Fact]
        public async Task PostGraphQL_Mutation_Update()
        {
            using (var client = new HttpClient())
            {
                var patch = @"
                    mutation {
                          updateFamilies( input: {
                            id: 4
                            patch: {
                              familyName: Habboo
                            }
                          }) {
                              families {
                                id
                              }
                            }
                    }";

                var contentMutation = new StringContent(patch, Encoding.UTF8, "application/graphql");
                var resultMutation = await client.PostAsync($"{_fixture.BaseUrl}/graphql", contentMutation);

                Assert.Equal(HttpStatusCode.OK, resultMutation.StatusCode);

                var q = @"
                    query {
                          families(id: 4) {
                            familyName
                            parents(id: 1) {
                              name
                              work {
                                companyName
                              }
                            }
                          }
                    }";

                var contentQuery = new StringContent(q, Encoding.UTF8, "application/graphql");
                var resultQuery = await client.PostAsync($"{_fixture.BaseUrl}/graphql", contentQuery);

                Assert.Equal(HttpStatusCode.OK, resultQuery.StatusCode);

                var data = JsonConvert.DeserializeObject<JObject>(await resultQuery.Content.ReadAsStringAsync());
                Assert.NotNull(data["data"]);
                Assert.Equal("Habboo", data["data"]["families"][0]["familyName"].Value<string>());
            }
        }

        [Fact]
        public async Task PostGraphQL_Mutation_Add_Delete()
        {
            using (var client = new HttpClient())
            {
                var add = @"
                    mutation {
                          addFamilies ( input: {
                            families: {
                              familyName: Newtons
                              notes: Hello
                            }
                          }) {
                            families {
                              id
                              familyName
                              children {
                                name
                                age
                              }
                            }
                          }
                    }";

                var contentMutation = new StringContent(add, Encoding.UTF8, "application/graphql");
                var resultMutation = await client.PostAsync($"{_fixture.BaseUrl}/graphql", contentMutation);

                Assert.Equal(HttpStatusCode.OK, resultMutation.StatusCode);

                var addResult = JsonConvert.DeserializeObject<JObject>(await resultMutation.Content.ReadAsStringAsync());

                var id = addResult["data"]["families"]["id"].Value<string>();

                var q = @"
                    query {
                          families(id: " + id + @") {
                            familyName
                            parents(id: 1) {
                              name
                              work {
                                companyName
                              }
                            }
                          }
                    }";

                var contentQuery = new StringContent(q, Encoding.UTF8, "application/graphql");
                var resultQuery = await client.PostAsync($"{_fixture.BaseUrl}/graphql", contentQuery);

                Assert.Equal(HttpStatusCode.OK, resultQuery.StatusCode);

                var data = JsonConvert.DeserializeObject<JObject>(await resultQuery.Content.ReadAsStringAsync());
                Assert.NotNull(data["data"]);
                Assert.Equal("Newtons", data["data"]["families"][0]["familyName"].Value<string>());

                // Delete

                var delete = @"
                    mutation {
                          deleteFamilies ( input: {
                            id: " + id + @"
                          })
                    }";

                var deleteMutaiton = new StringContent(delete, Encoding.UTF8, "application/graphql");
                var resultDelete = await client.PostAsync($"{_fixture.BaseUrl}/graphql", deleteMutaiton);

                Assert.Equal(HttpStatusCode.OK, resultDelete.StatusCode);

                data = JsonConvert.DeserializeObject<JObject>(await resultDelete.Content.ReadAsStringAsync());

                Assert.True(data["data"]["Result"].Value<bool>());

                // Try to fetch deleted data

                contentQuery = new StringContent(q, Encoding.UTF8, "application/graphql");
                resultQuery = await client.PostAsync($"{_fixture.BaseUrl}/graphql", contentQuery);

                Assert.Equal(HttpStatusCode.OK, resultQuery.StatusCode);

                data = JsonConvert.DeserializeObject<JObject>(await resultQuery.Content.ReadAsStringAsync());
                Assert.NotNull(data["data"]);
                Assert.Empty(data["data"]["families"]);
            }
        }

        [Fact]
        public async Task PostGraphQL_Filter_Bool()
        {
            using (var client = new HttpClient())
            {
                var q = @"
                    query {
                          families {
                            id
                            bankAccount(isActive: true) {
                              balance
                            }
                          }
                    }";

                var content = new StringContent(q, Encoding.UTF8, "application/graphql");
                var result = await client.PostAsync($"{_fixture.BaseUrl}/graphql", content);

                Assert.Equal(HttpStatusCode.OK, result.StatusCode);

                var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

                Assert.True(data["data"]["families"][0]["id"].ToString() == "1");
                Assert.Equal(9, data["data"]["families"].Count());
                Assert.Null(data["errors"]);
            }
        }

        [Fact]
        public async Task PostGraphQL_Json()
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(@"{""query"":""{users}""}", Encoding.UTF8, "application/json");
                var result = await client.PostAsync($"{_fixture.BaseUrl}/graphql", content);

                Assert.Equal(HttpStatusCode.OK, result.StatusCode);

                var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

                Assert.NotNull(data["data"]);
                Assert.Null(data["errors"]);
            }
        }

        [Fact]
        public async Task PostGraphQL_Error_InvalidJson()
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent("{ users }", Encoding.UTF8, "application/json");
                var result = await client.PostAsync($"{_fixture.BaseUrl}/graphql", content);
                Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            }
        }

        [Fact]
        public async Task PostGraphQL_Error_MissingQuery()
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(@"{ }", Encoding.UTF8, "application/json");
                var result = await client.PostAsync($"{_fixture.BaseUrl}/graphql", content);
                Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            }
        }

        [Fact]
        public async Task GetGraphQL_NotImplemented()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"{_fixture.BaseUrl}/graphql?query={{users}}");

                Assert.Equal(HttpStatusCode.NotImplemented, result.StatusCode);

                var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

                Assert.Null(data["data"]);
                Assert.NotNull(data["errors"]);
            }
        }
    }
}