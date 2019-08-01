using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FakeServer.Test
{
    // Tests in the same collection are not run in parallel
    // After each test data should be in the same state as in the beginning of the test or future tests might fail
    [Collection("Integration collection")]
    [Trait("category", "integration")]
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
            var result = await _fixture.Client.GetAsync($"api/");
            result.EnsureSuccessStatusCode();

            var collections = JsonConvert.DeserializeObject<IEnumerable<string>>(await result.Content.ReadAsStringAsync());
            Assert.True(collections.Count() > 0);
        }

        [Fact]
        public async Task GetUsers()
        {
            var result = await _fixture.Client.GetAsync($"api/users");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(4, items.Count());
        }

        [Fact]
        public async Task GetUsers_Accept_CSV()
        {
            var request = new HttpRequestMessage(new HttpMethod("GET"), $"api/users");
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/csv"));

            var result = await _fixture.Client.SendAsync(request);
            result.EnsureSuccessStatusCode();

            var rows = await result.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(rows));

            var items = rows.Split(Environment.NewLine);
            Assert.Contains("4,Jarvis,52,SF,Autocar Company,SF,", items[3]);
        }

        [Fact]
        public async Task GetSingleFamily_Accept_CSV()
        {
            var request = new HttpRequestMessage(new HttpMethod("GET"), $"api/families/1");
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/csv"));

            var result = await _fixture.Client.SendAsync(request);
            result.EnsureSuccessStatusCode();

            var rows = await result.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(rows));

            var items = rows.Split(Environment.NewLine);
            Assert.Single(items);
            // Check that inner collections are serialized correctly
            Assert.DoesNotContain("System.Collections.Generic.List", items[0]);
        }

        [Fact]
        public async Task GetUsers_Accept_XML()
        {
            var request = new HttpRequestMessage(new HttpMethod("GET"), $"api/users");
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/xml"));

            var result = await _fixture.Client.SendAsync(request);
            result.EnsureSuccessStatusCode();

            var rows = await result.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(rows));
        }

        [Fact]
        public async Task GetSingleFamily_Accept_XML()
        {
            var request = new HttpRequestMessage(new HttpMethod("GET"), $"api/families/1");
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/xml"));

            var result = await _fixture.Client.SendAsync(request);
            result.EnsureSuccessStatusCode();

            var xml = await result.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(xml));

            // Check that inner collections are serialized correctly
            Assert.DoesNotContain("[id, 0]", xml);
            Assert.DoesNotContain("System.Collections.Generic.List", xml);

            var rows = xml.Split(Environment.NewLine);
            Assert.Equal("<family>", rows[0].Trim());
            Assert.Equal("<parents>", rows[3].Trim());
            Assert.Equal("<parent>", rows[4].Trim());
            Assert.Equal("<children>", rows[33].Trim());
            Assert.Equal("<child>", rows[34].Trim());
            Assert.Equal("<work>", rows[12].Trim());
        }

        [Fact]
        public async Task GetUsers_Accept_NotAcceptable()
        {
            var request = new HttpRequestMessage(new HttpMethod("GET"), $"api/users");
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

            var result = await _fixture.Client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotAcceptable, result.StatusCode);
        }

        [Fact]
        public async Task GetUsers_SkipTake()
        {
            var result = await _fixture.Client.GetAsync($"api/users?skip=1&take=2");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(2, items.Count());
            Assert.Equal("2", items.First()["id"]);
        }

        [Fact]
        public async Task GetUsers_Skip()
        {
            var result = await _fixture.Client.GetAsync($"api/users?skip=1");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(3, items.Count());
            Assert.Equal("2", items.First()["id"]);
        }

        [Fact]
        public async Task GetUsers_Take()
        {
            var result = await _fixture.Client.GetAsync($"api/users?take=2");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(2, items.Count());
            Assert.Equal("1", items.First()["id"]);
        }

        [Fact]
        public async Task GetUsers_OffsetLimit()
        {
            var result = await _fixture.Client.GetAsync($"api/users?offset=1&limit=2");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(2, items.Count());
            Assert.Equal("2", items.First()["id"]);
        }

        [Fact]
        public async Task GetUsers_Offset()
        {
            var result = await _fixture.Client.GetAsync($"api/users?offset=1");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(3, items.Count());
            Assert.Equal("2", items.First()["id"]);
        }

        [Fact]
        public async Task GetUsers_Limit()
        {
            var result = await _fixture.Client.GetAsync($"api/users?limit=2");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(2, items.Count());
            Assert.Equal("1", items.First()["id"]);
        }

        [Fact]
        public async Task GetUsers_Page_And_Per_Page()
        {
            var result = await _fixture.Client.GetAsync($"api/users?page=2&per_page=2");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(2, items.Count());
            Assert.Equal("3", items.First()["id"]);
        }

        [Fact]
        public async Task GetUsers_Page()
        {
            var result = await _fixture.Client.GetAsync($"api/users?page=1");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(4, items.Count());
            Assert.Equal("1", items.First()["id"]);
        }

        [Fact]
        public async Task GetUsers_Per_Page()
        {
            var result = await _fixture.Client.GetAsync($"api/users?per_page=2");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(2, items.Count());
            Assert.Equal("1", items.First()["id"]);
        }

        [Fact]
        public async Task GetUsers_PageDoesNotExist_ReturnsEmptyList()
        {
            var result = await _fixture.Client.GetAsync($"api/users?page=4");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Empty(items);
        }

        [Theory]
        [InlineData("skip=2&limit=10")]
        [InlineData("skip=2&per_page=10")]
        [InlineData("offset=2&take=10")]
        [InlineData("offset=2&per_page=10")]
        [InlineData("page=2&take=10")]
        [InlineData("page=2&limit=10")]
        [InlineData("take=10&limit=10")]
        [InlineData("take=10&per_page=10")]
        [InlineData("skip=2&offset=2&limit=10")]
        [InlineData("take=10&offset=2&limit=10")]
        [InlineData("skip=2&page=2&per_page=10")]
        [InlineData("take=10&page=2&per_page=10")]
        [InlineData("offset=2&page=2&per_page=10")]
        [InlineData("limit=10&page=2&per_page=10")]
        [InlineData("page=1&skip=2&take=10")]
        [InlineData("per_page=10&skip=2&take=10")]
        [InlineData("page=1&offset=2&limit=10")]
        [InlineData("per_page=10&offset=2&limit=10")]
        [InlineData("skip=2&take=2&offset=2&limit=10")]
        [InlineData("offset=2&limit=10&page=2&per_page=10")]
        [InlineData("skip=2&take=2&offset=2&limit=10&page=2&per_page=10")]
        public async Task GetUsers_InvalidPagingCombinations_BadRequest(string queryParams)
        {
            var result = await _fixture.Client.GetAsync($"api/users?{queryParams}");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Theory]
        [InlineData("sort=location")]
        [InlineData("sort=-location")]
        public async Task GetUsers_SortByLocationDescending(string queryParams)
        {
            var result = await _fixture.Client.GetAsync($"api/users?{queryParams}");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(4, items.Count());
            Assert.Equal("SF", items.ToList()[0]["location"]);
            Assert.Equal("SF", items.ToList()[1]["location"]);
            Assert.Equal("NY", items.ToList()[2]["location"]);
        }

        [Theory]
        [InlineData("sort=+location")]
        [InlineData("sort= location")]
        public async Task GetUsers_SortByLocationAscending(string queryParams)
        {
            var result = await _fixture.Client.GetAsync($"api/users?{queryParams}");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(4, items.Count());
            Assert.Equal("London", items.ToList()[0]["location"]);
            Assert.Equal("NY", items.ToList()[1]["location"]);
            Assert.Equal("SF", items.ToList()[2]["location"]);
        }

        [Theory]
        [InlineData("sort=location,age")]
        [InlineData("sort=location,-age")]
        public async Task GetUsers_SortByLocationAndAgeDescending(string queryParams)
        {
            var result = await _fixture.Client.GetAsync($"api/users?{queryParams}");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(4, items.Count());
            Assert.Equal("SF", items.ToList()[0]["location"]);
            Assert.Equal("SF", items.ToList()[1]["location"]);
            Assert.Equal("NY", items.ToList()[2]["location"]);

            Assert.Equal("52", items.ToList()[0]["age"]);
            Assert.Equal("30", items.ToList()[1]["age"]);
        }

        [Theory]
        [InlineData("sort=location,+age")]
        [InlineData("sort=location, age")]
        public async Task GetUsers_SortByLocationDefaultAndAgeAscending(string queryParams)
        {
            var result = await _fixture.Client.GetAsync($"api/users?{queryParams}");
            result.EnsureSuccessStatusCode();

            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(4, items.Count());
            Assert.Equal("SF", items.ToList()[0]["location"]);
            Assert.Equal("SF", items.ToList()[1]["location"]);
            Assert.Equal("NY", items.ToList()[2]["location"]);

            Assert.Equal("30", items.ToList()[0]["age"]);
            Assert.Equal("52", items.ToList()[1]["age"]);
        }

        [Fact]
        public async Task GetItem_WithId_Found()
        {
            var result = await _fixture.Client.GetAsync($"api/users/1");
            Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);

            var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal("James", item["name"].Value<string>());
        }

        [Fact]
        public async Task GetItem_WithId_NotFound()
        {
            var result = await _fixture.Client.GetAsync($"api/users/100000");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task GetItem_QueryParams()
        {
            var result = await _fixture.Client.GetAsync($"api/users?name=Phil&age=25&take=5");
            var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal("Phil", allUsers[0]["name"].Value<string>());
            Assert.Equal("Box Company", allUsers[0]["work"]["name"].Value<string>());
        }

        [Fact]
        public async Task GetItem_QueryParamsWithPer_Page()
        {
            var result = await _fixture.Client.GetAsync($"api/users?name=Phil&age=25&per_page=5");
            var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal("Phil", allUsers[0]["name"].Value<string>());
            Assert.Equal("Box Company", allUsers[0]["work"]["name"].Value<string>());
        }

        [Fact]
        public async Task GetItem_PropertyNotFound()
        {
            var result = await _fixture.Client.GetAsync($"api/users?notfound=AA");
            var foundUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Empty(foundUsers);
        }

        [Fact]
        public async Task GetItem_QueryFilters_NotEqual()
        {
            var result = await _fixture.Client.GetAsync($"api/users?age_ne=25");
            var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal(3, allUsers.Count());
        }

        [Fact]
        public async Task GetItem_QueryFilters_LessThan()
        {
            var result = await _fixture.Client.GetAsync($"api/users?age_lt=30");
            var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Single(allUsers);
        }

        [Fact]
        public async Task GetItem_QueryFilters_LessThanEquals()
        {
            var result = await _fixture.Client.GetAsync($"api/users?age_lte=30");
            var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal(2, allUsers.Count());
        }

        [Fact]
        public async Task GetItem_QueryFilters_GreaterThan()
        {
            var result = await _fixture.Client.GetAsync($"api/users?work.rating_gt=1.6");
            var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal(2, allUsers.Count());
        }

        [Fact]
        public async Task GetItem_QueryFilters_GreaterThanEquals()
        {
            var result = await _fixture.Client.GetAsync($"api/users?work.rating_gte=1.6");
            var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal(3, allUsers.Count());
        }

        [Fact]
        public async Task GetItem_QueryFilters_DateTime()
        {
            var result = await _fixture.Client.GetAsync($"api/families?bankAccount.opened_lt=1/1/2015");
            var allFamilies = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal(8, allFamilies.Count());
        }

        [Fact]
        public async Task GetItem_QueryFilters_Bool()
        {
            var result = await _fixture.Client.GetAsync($"api/families?bankAccount.isActive=true");
            var allFamilies = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal(9, allFamilies.Count());
        }

        [Fact]
        public async Task GetItem_Fields()
        {
            var result = await _fixture.Client.GetAsync($"api/users?fields=age,name");
            var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal(2, allUsers[0].Count());
            Assert.NotNull(allUsers[0]["age"]);
            Assert.NotNull(allUsers[0]["name"]);
            Assert.Null(allUsers[0]["id"]);
        }

        [Fact]
        public async Task GetItem_TextSearch_BadRequest()
        {
            var result = await _fixture.Client.GetAsync($"api/families?q=some&k=fail");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task GetItem_TextSearch()
        {
            var result = await _fixture.Client.GetAsync($"api/families?q=Hillsboro");
            var allFamilies = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal(3, allFamilies.Count());
        }

        [Fact]
        public async Task GetItem_Nested()
        {
            var result = await _fixture.Client.GetAsync($"api/families/1/address/country");
            var country = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal("Brazil", country["name"].Value<string>());
        }

        [Fact]
        public async Task GetItem_Nested_MultipleId()
        {
            var result = await _fixture.Client.GetAsync($"api/families/18/parents/1/work");
            var workplace = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal("EVIDENDS", workplace["companyName"].Value<string>());
        }

        [Fact]
        public async Task PatchItem()
        {
            // Original { "id": 1, "name": "James", "age": 40, "location": "NY", "work": { "name": "ACME", "location": "NY" } },
            var patchData = new { name = "Albert", age = 12, work = new { name = "EMACS" } };

            var content = new StringContent(JsonConvert.SerializeObject(patchData), Encoding.UTF8, "application/json+merge-patch");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"api/users/1") { Content = content };
            var result = await _fixture.Client.SendAsync(request);
            result.EnsureSuccessStatusCode();

            result = await _fixture.Client.GetAsync($"api/users/1"); ;
            result.EnsureSuccessStatusCode();
            var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal(patchData.name, item["name"].Value<string>());
            Assert.Equal(patchData.age, item["age"].Value<int>());
            Assert.Equal("NY", item["location"].Value<string>());
            Assert.Equal("EMACS", item["work"]["name"].Value<string>());
            Assert.Equal("NY", item["work"]["location"].Value<string>());

            result = await _fixture.Client.GetAsync($"api/users");
            result.EnsureSuccessStatusCode();
            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(4, items.Count());

            content = new StringContent(JsonConvert.SerializeObject(new { name = "James", age = 40, work = new { name = "ACME" } }), Encoding.UTF8, "application/merge-patch+json");
            request = new HttpRequestMessage(new HttpMethod("PATCH"), $"api/users/1") { Content = content };
            result = await _fixture.Client.SendAsync(request);
            result.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task PatchItem_UnsupportedMediaType()
        {
            var patchData = new { name = "Albert", age = 12, work = new { name = "EMACS" } };

            var content = new StringContent(JsonConvert.SerializeObject(patchData), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"api/users/1") { Content = content };
            var result = await _fixture.Client.SendAsync(request);

            Assert.Equal(HttpStatusCode.UnsupportedMediaType, result.StatusCode);
        }

        [Fact]
        public async Task PostAndDeleteItem_ExistingCollection()
        {
            // Try with "wrong" id
            var newUser = new { id = 8, name = "Newton" };

            var content = new StringContent(JsonConvert.SerializeObject(newUser), Encoding.UTF8, "application/json");
            var result = await _fixture.Client.PostAsync($"api/users", content);
            result.EnsureSuccessStatusCode();
            var postResponse = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal($@"{_fixture.Client.BaseAddress}api/users/{postResponse["id"]}", result.Headers.Location.OriginalString);

            result = await _fixture.Client.GetAsync($"api/users/{postResponse["id"]}");
            result.EnsureSuccessStatusCode();
            var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal(newUser.name, item["name"].Value<string>());

            result = await _fixture.Client.GetAsync($"api/users");
            result.EnsureSuccessStatusCode();
            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(5, items.Count());

            result = await _fixture.Client.DeleteAsync($"api/users/{postResponse["id"]}");
            result.EnsureSuccessStatusCode();

            result = await _fixture.Client.GetAsync($"api/users");
            result.EnsureSuccessStatusCode();
            items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(4, items.Count());
        }

        [Fact]
        public async Task PostAndDeleteItem_NewCollection()
        {
            var newUser = new { id = 256, name = "Newton" };

            var result = await _fixture.Client.GetAsync($"api/");
            result.EnsureSuccessStatusCode();
            var collections = JsonConvert.DeserializeObject<IEnumerable<string>>(await result.Content.ReadAsStringAsync());
            var originalAmount = collections.Count();

            var content = new StringContent(JsonConvert.SerializeObject(newUser), Encoding.UTF8, "application/json");
            result = await _fixture.Client.PostAsync($"api/hello", content);
            result.EnsureSuccessStatusCode();

            result = await _fixture.Client.GetAsync($"api");
            result.EnsureSuccessStatusCode();
            collections = JsonConvert.DeserializeObject<IEnumerable<string>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(originalAmount + 1, collections.Count());

            result = await _fixture.Client.GetAsync($"api/hello/256");
            result.EnsureSuccessStatusCode();
            var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal(newUser.name, item["name"].Value<string>());

            result = await _fixture.Client.GetAsync($"api/hello");
            result.EnsureSuccessStatusCode();
            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Single(items);

            result = await _fixture.Client.DeleteAsync($"api/hello/256");
            result.EnsureSuccessStatusCode();

            result = await _fixture.Client.GetAsync($"api/hello");
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task PostAndDeleteItem_NewCollection_NoId()
        {
            var newUser = new { name = "Newton" };

            var result = await _fixture.Client.GetAsync($"api/");
            result.EnsureSuccessStatusCode();
            var collections = JsonConvert.DeserializeObject<IEnumerable<string>>(await result.Content.ReadAsStringAsync());
            var originalAmount = collections.Count();

            var content = new StringContent(JsonConvert.SerializeObject(newUser), Encoding.UTF8, "application/json");
            result = await _fixture.Client.PostAsync($"api/hello", content);
            result.EnsureSuccessStatusCode();

            result = await _fixture.Client.GetAsync($"api");
            result.EnsureSuccessStatusCode();
            collections = JsonConvert.DeserializeObject<IEnumerable<string>>(await result.Content.ReadAsStringAsync());
            Assert.Equal(originalAmount + 1, collections.Count());

            result = await _fixture.Client.GetAsync($"api/hello/0");
            result.EnsureSuccessStatusCode();
            var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal(newUser.name, item["name"].Value<string>());

            result = await _fixture.Client.GetAsync($"api/hello");
            result.EnsureSuccessStatusCode();
            var items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(await result.Content.ReadAsStringAsync());
            Assert.Single(items);

            result = await _fixture.Client.DeleteAsync($"api/hello/0");
            result.EnsureSuccessStatusCode();

            result = await _fixture.Client.GetAsync($"api/hello");
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task Post_SingleItem_ExistingCollection()
        {
            var collection = "configuration";
            var newConfig = new { value = "Something new" };

            var content = new StringContent(JsonConvert.SerializeObject(newConfig), Encoding.UTF8, "application/json");
            var result = await _fixture.Client.PostAsync($"api/{collection}", content);
            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        }

        [Fact]
        public async Task Put_SingleItem_ExistingCollection_BadRequest()
        {
            var collection = "configuration";
            var newConfig = new { value = "Something new" };

            var content = new StringContent(JsonConvert.SerializeObject(newConfig), Encoding.UTF8, "application/json");
            var result = await _fixture.Client.PutAsync($"api/{collection}/0", content);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task Put_SingleItem_ExistingItem()
        {
            var collection = "configuration";
            var newConfig = new { value = "Something new" };

            var content = new StringContent(JsonConvert.SerializeObject(newConfig), Encoding.UTF8, "application/json");
            var result = await _fixture.Client.PutAsync($"api/{collection}", content);
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

            result = await _fixture.Client.GetAsync($"api/{collection}");
            result.EnsureSuccessStatusCode();
            var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal(newConfig.value, item["value"].Value<string>());
        }

        [Fact]
        public async Task Put_CollectionItem_ExistingItem()
        {
            var collection = "users";
            var newUser = new { id = 1, name = "Newton" };

            var content = new StringContent(JsonConvert.SerializeObject(newUser), Encoding.UTF8, "application/json");
            var result = await _fixture.Client.PutAsync($"api/{collection}", content);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task Patch_SingleItem()
        {
            var collection = "configuration_for_patch";
            var newConfig = new { url = "192.168.0.1" };

            var content = new StringContent(JsonConvert.SerializeObject(newConfig), Encoding.UTF8, "application/json+merge-patch");
            var result = await _fixture.Client.PatchAsync($"api/{collection}", content);
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

            result = await _fixture.Client.GetAsync($"api/{collection}");
            result.EnsureSuccessStatusCode();
            var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal(newConfig.url, item["url"].Value<string>());
            Assert.Equal("abba", item["password"].Value<string>());
        }

        [Fact]
        public async Task Delete_SingleItem()
        {
            var collection = "configuration_for_delete";
            var newConfig = new { value = "Something new" };

            var result = await _fixture.Client.DeleteAsync($"api/{collection}");
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

            result = await _fixture.Client.GetAsync($"api/{collection}");
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task WebSockets_CalledTwice()
        {
            var webSocketMessages = new List<dynamic>();

            var webSocket = await _fixture.CreateWebSocketClient();

            async Task WebSocketReceiveHandler()
            {
                var buffer = new byte[1024 * 4];
                await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                var count = Array.IndexOf(buffer, (byte)0);
                count = count == -1 ? buffer.Length : count;
                var message = Encoding.Default.GetString(buffer, 0, count);
                webSocketMessages.Add(JsonConvert.DeserializeObject<dynamic>(message));
            }

            var patchData = new { name = "Albert", age = 12, work = new { name = "EMACS" } };

            var content = new StringContent(JsonConvert.SerializeObject(patchData), Encoding.UTF8, "application/json+merge-patch");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"api/users/1") { Content = content };
            var result = await _fixture.Client.SendAsync(request);
            result.EnsureSuccessStatusCode();

            await WebSocketReceiveHandler();

            content = new StringContent(JsonConvert.SerializeObject(new { name = "James", age = 40, work = new { name = "ACME" } }), Encoding.UTF8, "application/json+merge-patch");
            request = new HttpRequestMessage(new HttpMethod("PATCH"), $"api/users/1") { Content = content };
            result = await _fixture.Client.SendAsync(request);
            result.EnsureSuccessStatusCode();

            await WebSocketReceiveHandler();

            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test finished", CancellationToken.None);

            Assert.Equal(2, webSocketMessages.Count);
            Assert.Equal("PATCH", webSocketMessages[0].method.ToString());
            Assert.Equal("/api/users/1", webSocketMessages[0].path.ToString());
        }

        [Fact]
        public async Task Async_PostPutPatchDelete()
        {
            async Task<HttpResponseMessage> GetWhenStatusNotOk(System.Uri pQueueUrl)
            {
                using (var c = _fixture.CreateClient(false))
                {
                    while (true)
                    {
                        var response = await c.GetAsync(pQueueUrl);

                        if (response.StatusCode != HttpStatusCode.OK)
                            return response;

                        await Task.Delay(100);
                    }
                }
            }

            var newBook = new { title = "Adventures of Robin Hood" };

            // POST

            var content = new StringContent(JsonConvert.SerializeObject(newBook), Encoding.UTF8, "application/json");
            var result = await _fixture.Client.PostAsync($"async/book", content);
            result.EnsureSuccessStatusCode();

            var queueUrl = result.Headers.Location;

            result = await _fixture.Client.GetAsync($"{queueUrl}NOTFOUND");
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);

            HttpResponseMessage resultForAction = await GetWhenStatusNotOk(queueUrl);

            Assert.Equal(HttpStatusCode.SeeOther, resultForAction.StatusCode);
            Assert.Equal($"{_fixture.Client.BaseAddress}api/book/0", resultForAction.Headers.Location.ToString());

            result = await _fixture.Client.GetAsync(resultForAction.Headers.Location);
            result.EnsureSuccessStatusCode();
            var item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal(newBook.title, item["title"].Value<string>());

            // PUT

            var updateBook = new { title = "Adventures of Sherlock Holmes" };

            content = new StringContent(JsonConvert.SerializeObject(updateBook), Encoding.UTF8, "application/json");
            result = await _fixture.Client.PutAsync($"async/book/0", content);
            result.EnsureSuccessStatusCode();

            queueUrl = result.Headers.Location;

            resultForAction = await GetWhenStatusNotOk(queueUrl);
            Assert.Equal(HttpStatusCode.SeeOther, resultForAction.StatusCode);

            result = await _fixture.Client.GetAsync(resultForAction.Headers.Location);
            result.EnsureSuccessStatusCode();
            item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal(updateBook.title, item["title"].Value<string>());

            // PATCH

            var patchBook = new { author = "Edgar Allen Poe" };

            content = new StringContent(JsonConvert.SerializeObject(patchBook), Encoding.UTF8, "application/json+merge-patch");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"async/book/0") { Content = content };
            result = await _fixture.Client.SendAsync(request);
            result.EnsureSuccessStatusCode();

            queueUrl = result.Headers.Location;

            resultForAction = await GetWhenStatusNotOk(queueUrl);
            Assert.Equal(HttpStatusCode.SeeOther, resultForAction.StatusCode);

            result = await _fixture.Client.GetAsync(resultForAction.Headers.Location);
            result.EnsureSuccessStatusCode();
            item = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal(patchBook.author, item["author"].Value<string>());
            Assert.Equal(updateBook.title, item["title"].Value<string>());

            // DELETE

            result = await _fixture.Client.DeleteAsync($"async/book/0");
            result.EnsureSuccessStatusCode();

            queueUrl = result.Headers.Location;

            resultForAction = await GetWhenStatusNotOk(queueUrl);

            Assert.Equal(HttpStatusCode.SeeOther, resultForAction.StatusCode);
            Assert.Equal($"{_fixture.Client.BaseAddress}api/book/0", resultForAction.Headers.Location.ToString());

            // DELETE JOB

            result = await _fixture.Client.DeleteAsync(queueUrl);
            result.EnsureSuccessStatusCode();

            result = await _fixture.Client.GetAsync(queueUrl);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task GetOptions()
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

            var request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"api");
            var result = await _fixture.Client.SendAsync(request);
            Assert.Equal("GET, HEAD, POST, OPTIONS", GetAllow(result));

            request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"api/users");
            result = await _fixture.Client.SendAsync(request);
            Assert.Equal("GET, HEAD, POST, OPTIONS", GetAllow(result));

            request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"api/users/22");
            result = await _fixture.Client.SendAsync(request);
            Assert.Equal("GET, HEAD, POST, PUT, PATCH, DELETE, OPTIONS", GetAllow(result));

            request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"async/users");
            result = await _fixture.Client.SendAsync(request);
            Assert.Equal("POST, OPTIONS", GetAllow(result));

            request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"async/users/22");
            result = await _fixture.Client.SendAsync(request);
            Assert.Equal("PUT, PATCH, DELETE, OPTIONS", GetAllow(result));

            request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"async/queue/22");
            result = await _fixture.Client.SendAsync(request);
            Assert.Equal("GET, DELETE, OPTIONS", GetAllow(result));
        }

        [Fact]
        public async Task GetHead_Families_Count()
        {
            var request = new HttpRequestMessage(new HttpMethod("HEAD"), $"api/families");
            var result = await _fixture.Client.SendAsync(request);
            result.EnsureSuccessStatusCode();

            var countHeader = result.Headers.GetValues("X-Total-Count").First();
            Assert.Equal("20", countHeader);

            var body = await result.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, body);
        }

        [Fact]
        public async Task GetHead_NotFound()
        {
            var request = new HttpRequestMessage(new HttpMethod("HEAD"), $"api/users/2000");
            var result = await _fixture.Client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);

            var body = await result.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, body);
        }

        [Fact]
        public async Task GetHead_ETag()
        {
            var requestHead = new HttpRequestMessage(new HttpMethod("HEAD"), $"api/families");
            var resultHead = await _fixture.Client.SendAsync(requestHead);
            resultHead.EnsureSuccessStatusCode();

            var requestGet = new HttpRequestMessage(new HttpMethod("GET"), $"api/families");
            var resultGet = await _fixture.Client.SendAsync(requestGet);
            resultGet.EnsureSuccessStatusCode();

            var etagHeaderHead = resultHead.Headers.GetValues("ETag").First();
            Assert.False(string.IsNullOrEmpty(etagHeaderHead));

            var etagHeaderGet = resultGet.Headers.GetValues("ETag").First();
            Assert.False(string.IsNullOrEmpty(etagHeaderGet));

            Assert.Equal(etagHeaderHead, etagHeaderGet);
        }

        [Fact]
        public async Task GetPaginationHeaders()
        {
            var result = await _fixture.Client.GetAsync($"api/families?skip=7&take=4");
            result.EnsureSuccessStatusCode();

            var countHeader = result.Headers.GetValues("X-Total-Count").First();
            Assert.Equal("20", countHeader);

            var linksHeaders = result.Headers.GetValues("Link").First();
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?skip=3&take=4>; rel=""prev""", linksHeaders);
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?skip=11&take=4>; rel=""next""", linksHeaders);
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?skip=0&take=4>; rel=""first""", linksHeaders);
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?skip=16&take=4>; rel=""last""", linksHeaders);

            result = await _fixture.Client.GetAsync($"api/families?skip=0&take=21");

            countHeader = result.Headers.GetValues("X-Total-Count").First();
            Assert.Equal("20", countHeader);

            linksHeaders = result.Headers.GetValues("Link").First();
            Assert.True(string.IsNullOrEmpty(linksHeaders));

            result = await _fixture.Client.GetAsync($"api/families?skip=0&take=10");

            linksHeaders = result.Headers.GetValues("Link").First();
            Assert.DoesNotContain("prev", linksHeaders);
            Assert.DoesNotContain("first", linksHeaders);
            Assert.Contains("next", linksHeaders);
            Assert.Contains("last", linksHeaders);
        }

        [Fact]
        public async Task GetPaginationHeaders_offset_limit()
        {
            var result = await _fixture.Client.GetAsync($"api/families?offset=2&limit=17");

            var countHeader = result.Headers.GetValues("X-Total-Count").First();
            Assert.Equal("20", countHeader);

            var linksHeaders = result.Headers.GetValues("Link").First();
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?offset=0&limit=2>; rel=""prev""", linksHeaders);
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?offset=19&limit=17>; rel=""next""", linksHeaders);
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?offset=0&limit=17>; rel=""first""", linksHeaders);
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?offset=3&limit=17>; rel=""last""", linksHeaders);
        }

        [Fact]
        public async Task GetPaginationHeaders_page_per_page()
        {
            var result = await _fixture.Client.GetAsync($"api/families?page=2&per_page=5");

            var countHeader = result.Headers.GetValues("X-Total-Count").First();
            Assert.Equal("20", countHeader);

            var linksHeaders = result.Headers.GetValues("Link").First();
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?page=1&per_page=5>; rel=""prev""", linksHeaders);
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?page=3&per_page=5>; rel=""next""", linksHeaders);
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?page=1&per_page=5>; rel=""first""", linksHeaders);
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?page=4&per_page=5>; rel=""last""", linksHeaders);
        }

        [Fact]
        public async Task GetPaginationHeaders_page_per_page_no_next_and_last()
        {
            var result = await _fixture.Client.GetAsync($"api/families?page=2&per_page=17");

            var countHeader = result.Headers.GetValues("X-Total-Count").First();
            Assert.Equal("20", countHeader);

            var linksHeaders = result.Headers.GetValues("Link").First();
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?page=1&per_page=17>; rel=""prev""", linksHeaders);
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?page=1&per_page=17>; rel=""first""", linksHeaders);
            Assert.DoesNotContain(@"rel=""next""", linksHeaders);
            Assert.DoesNotContain(@"rel=""last""", linksHeaders);
        }

        [Fact]
        public async Task GetPaginationHeaders_page_per_page_no_first_and_prev()
        {
            var result = await _fixture.Client.GetAsync($"api/families?page=1&per_page=17");

            var countHeader = result.Headers.GetValues("X-Total-Count").First();
            Assert.Equal("20", countHeader);

            var linksHeaders = result.Headers.GetValues("Link").First();
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?page=2&per_page=17>; rel=""next""", linksHeaders);
            Assert.Contains($@"<{_fixture.Client.BaseAddress}api/families?page=2&per_page=17>; rel=""last""", linksHeaders);
            Assert.DoesNotContain(@"rel=""first""", linksHeaders);
            Assert.DoesNotContain(@"rel=""prev""", linksHeaders);
        }

        [Fact]
        public async Task GetItem_ETag_Cached_NoHeader()
        {
            var result = await _fixture.Client.GetAsync($"api/users?name=Phil");
            var originalEtag = result.Headers.ETag.Tag;

            var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal("Phil", allUsers[0]["name"].Value<string>());
            Assert.Equal("Box Company", allUsers[0]["work"]["name"].Value<string>());

            var request = new HttpRequestMessage(new HttpMethod("GET"), $"api/users?name=Phil&age=25");

            result = await _fixture.Client.SendAsync(request);
            var newEtag = result.Headers.ETag.Tag;

            Assert.Equal(originalEtag, newEtag);

            allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal("Phil", allUsers[0]["name"].Value<string>());
            Assert.Equal("Box Company", allUsers[0]["work"]["name"].Value<string>());
        }

        [Fact]
        public async Task GetItem_ETag_Cached_IfNoneMatch_Header()
        {
            var result = await _fixture.Client.GetAsync($"api/users?name=Phil");
            var originalEtag = result.Headers.ETag.Tag;

            var allUsers = JsonConvert.DeserializeObject<JArray>(await result.Content.ReadAsStringAsync());
            Assert.Equal("Phil", allUsers[0]["name"].Value<string>());
            Assert.Equal("Box Company", allUsers[0]["work"]["name"].Value<string>());

            var request = new HttpRequestMessage(new HttpMethod("GET"), $"api/users?name=Phil&age=25");
            request.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(originalEtag));

            result = await _fixture.Client.SendAsync(request);
            var newEtag = result.Headers.ETag.Tag;

            Assert.Equal(originalEtag, newEtag);
            Assert.Equal(HttpStatusCode.NotModified, result.StatusCode);

            var content = await result.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, content);
        }

        [Fact]
        public async Task GetItem_ETag_Cached_Put()
        {
            var result = await _fixture.Client.GetAsync($"api/users/1");
            var originalEtag = result.Headers.ETag.Tag;

            var user = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal("James", user["name"].Value<string>());
            Assert.Equal("ACME", user["work"]["name"].Value<string>());

            user["work"]["name"] = "Other Company";
            var request = new HttpRequestMessage(new HttpMethod("PUT"), $"api/users/1");
            request.Content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
            request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(originalEtag));

            result = await _fixture.Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

            result = await _fixture.Client.GetAsync($"api/users/1");

            user = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal("James", user["name"].Value<string>());
            Assert.Equal("Other Company", user["work"]["name"].Value<string>());

            // Try to update with original Tag

            user["work"]["name"] = "No Company";
            var request2 = new HttpRequestMessage(new HttpMethod("PUT"), $"api/users/1");
            request2.Content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
            request2.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(originalEtag));

            result = await _fixture.Client.SendAsync(request2);
            Assert.Equal(HttpStatusCode.PreconditionFailed, result.StatusCode);

            result = await _fixture.Client.GetAsync($"api/users/1");

            user = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());
            Assert.Equal("James", user["name"].Value<string>());
            Assert.Equal("Other Company", user["work"]["name"].Value<string>());
        }

        [Fact]
        public async Task PostGraphQL()
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
            var result = await _fixture.Client.PostAsync($"graphql", content);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

            Assert.NotNull(data["data"]);
            Assert.Null(data["errors"]);
        }

        [Fact]
        public async Task PostGraphQL_Mutation_Update()
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
            var resultMutation = await _fixture.Client.PostAsync($"graphql", contentMutation);

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
            var resultQuery = await _fixture.Client.PostAsync($"graphql", contentQuery);

            Assert.Equal(HttpStatusCode.OK, resultQuery.StatusCode);

            var data = JsonConvert.DeserializeObject<JObject>(await resultQuery.Content.ReadAsStringAsync());
            Assert.NotNull(data["data"]);
            Assert.Equal("Habboo", data["data"]["families"][0]["familyName"].Value<string>());
        }

        [Fact]
        public async Task PostGraphQL_Mutation_Add_Delete()
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
            var resultMutation = await _fixture.Client.PostAsync($"graphql", contentMutation);

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
            var resultQuery = await _fixture.Client.PostAsync($"graphql", contentQuery);

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
            var resultDelete = await _fixture.Client.PostAsync($"graphql", deleteMutaiton);

            Assert.Equal(HttpStatusCode.OK, resultDelete.StatusCode);

            data = JsonConvert.DeserializeObject<JObject>(await resultDelete.Content.ReadAsStringAsync());

            Assert.True(data["data"]["Result"].Value<bool>());

            // Try to fetch deleted data

            contentQuery = new StringContent(q, Encoding.UTF8, "application/graphql");
            resultQuery = await _fixture.Client.PostAsync($"graphql", contentQuery);

            Assert.Equal(HttpStatusCode.OK, resultQuery.StatusCode);

            data = JsonConvert.DeserializeObject<JObject>(await resultQuery.Content.ReadAsStringAsync());
            Assert.NotNull(data["data"]);
            Assert.Empty(data["data"]["families"]);
        }

        [Fact]
        public async Task PostGraphQL_Filter_Bool()
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
            var result = await _fixture.Client.PostAsync($"graphql", content);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

            Assert.True(data["data"]["families"][0]["id"].ToString() == "1");
            Assert.Equal(9, data["data"]["families"].Count());
            Assert.Null(data["errors"]);
        }

        [Fact]
        public async Task PostGraphQL_Json()
        {
            var content = new StringContent(@"{""query"":""{users}""}", Encoding.UTF8, "application/json");
            var result = await _fixture.Client.PostAsync($"graphql", content);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

            Assert.NotNull(data["data"]);
            Assert.Null(data["errors"]);
        }

        [Fact]
        public async Task PostGraphQL_Error_InvalidJson()
        {
            var content = new StringContent("{ users }", Encoding.UTF8, "application/json");
            var result = await _fixture.Client.PostAsync($"graphql", content);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task PostGraphQL_Error_MissingQuery()
        {
            var content = new StringContent(@"{ }", Encoding.UTF8, "application/json");
            var result = await _fixture.Client.PostAsync($"graphql", content);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task PostGraphQL_QueryParameter()
        {
            var content = new StringContent("", Encoding.UTF8, "application/graphql");
            string query = @"{users{name}}";
            var result = await _fixture.Client.PostAsync($"graphql?query={query}", content);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

            Assert.NotNull(data["data"]);
            Assert.Null(data["errors"]);
        }

        [Fact]
        public async Task PostGraphQL_Error_InvalidQueryParameter()
        {
            var content = new StringContent("", Encoding.UTF8, "application/graphql");
            string query = @"{ users { name";
            var result = await _fixture.Client.PostAsync($"graphql?query={query}", content);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task PostGraphQL_QueryParameter_VsBody()
        {
            var content = new StringContent(@"{ users { name } }", Encoding.UTF8, "application/graphql");
            string query = @"{ users { id, name } }";
            var result = await _fixture.Client.PostAsync($"graphql?query={query}", content);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

            // If the API had chosen to parse the request body instead of the `query` query parameter,
            // it would've only returned the name for each user
            Assert.NotNull(data["data"]["users"][0]["id"]);
            Assert.Null(data["errors"]);
        }

        [Fact]
        public async Task PostGraphQL_QueryParameter_ContentTypeNotImplemented()
        {
            var content = new StringContent("", Encoding.UTF8);
            string query = @"{ users { name } }";
            var result = await _fixture.Client.PostAsync($"graphql?query={query}", content);

            Assert.Equal(HttpStatusCode.NotImplemented, result.StatusCode);

            var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

            Assert.Null(data["data"]);
            Assert.NotNull(data["errors"]);
        }

        [Fact]
        public async Task GetGraphQL_Error_MissingQueryParameter()
        {
            var result = await _fixture.Client.GetAsync($"graphql");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task GetGraphQL_Error_InvalidQueryParameter()
        {
            string query = @"{ users { name";
            var result = await _fixture.Client.GetAsync($"graphql?query={query}");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task GetGraphQL_QueryParameter()
        {
            string query = @"{ users { name } }";
            var result = await _fixture.Client.GetAsync($"graphql?query={query}");

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

            Assert.NotNull(data["data"]);
            Assert.Null(data["errors"]);
        }
    }
}