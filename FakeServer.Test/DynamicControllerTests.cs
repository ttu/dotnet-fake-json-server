using FakeServer.Common;
using FakeServer.Controllers;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FakeServer.Test
{
    public class DynamicControllerTests
    {
        [Fact]
        public void GetCollections()
        {
            var filePath = UTHelpers.Up();
            var ds = new DataStore(filePath);
            var apiSettings = Options.Create(new ApiSettings());

            var controller = new DynamicController(ds, apiSettings);

            var collections = controller.GetCollections();
            Assert.Equal(3, collections.Count());

            UTHelpers.Down(filePath);
        }

        [Fact]
        public async Task PutItem_NoUpsert()
        {
            var filePath = UTHelpers.Up();
            var ds = new DataStore(filePath);
            var apiSettings = Options.Create(new ApiSettings { UpsertOnPut = false });

            var controller = new DynamicController(ds, apiSettings);

            var result = await controller.ReplaceItem("my_test", "2", JToken.Parse("{ 'id': 2, 'name': 'Raymond', 'age': 32 }"));
            Assert.IsType(typeof(NotFoundResult), result);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public async Task PutItem_Upsert()
        {
            var filePath = UTHelpers.Up();
            var ds = new DataStore(filePath);
            var apiSettings = Options.Create(new ApiSettings { UpsertOnPut = true });

            var controller = new DynamicController(ds, apiSettings);

            var result = await controller.ReplaceItem("my_test", "2", JToken.Parse("{ 'id': 2, 'name': 'Raymond', 'age': 32 }"));
            Assert.IsType(typeof(NoContentResult), result);

            var itemResult = controller.GetItem("my_test", "2");
            Assert.IsType(typeof(OkObjectResult), itemResult);

            var okObjectResult = itemResult as OkObjectResult;
            dynamic item = okObjectResult.Value as ExpandoObject;
            Assert.Equal("Raymond", item.name);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public async Task PutItem_Upsert_Id_String()
        {
            var filePath = UTHelpers.Up();
            var ds = new DataStore(filePath);
            var apiSettings = Options.Create(new ApiSettings { UpsertOnPut = true });

            var controller = new DynamicController(ds, apiSettings);

            var result = await controller.ReplaceItem("my_test_string", "acdc", JToken.Parse("{ 'id': 2, 'text': 'Hello' }")) as NoContentResult;
            Assert.Equal(204, result.StatusCode);

            var itemResult = controller.GetItem("my_test_string", "acdc") as OkObjectResult;

            dynamic item = itemResult.Value as ExpandoObject;
            Assert.Equal("acdc", item.id);
            Assert.Equal("Hello", item.text);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void GetItems_FavouriteMoveiWithQueryString()
        {
            var filePath = UTHelpers.Up();
            var ds = new DataStore(filePath);
            var apiSettings = Options.Create(new ApiSettings());

            var controller = new DynamicController(ds, apiSettings);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?parents.favouriteMovie=Predator");

            // NOTE: Can't but skip and take to querystring with tests
            var result = controller.GetItems("families", 0, 100) as OkObjectResult;
            Assert.Equal(11, ((IEnumerable<dynamic>)result.Value).Count());

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void GetItems_FriendsWithQueryString()
        {
            var filePath = UTHelpers.Up();
            var ds = new DataStore(filePath);
            var apiSettings = Options.Create(new ApiSettings());

            var controller = new DynamicController(ds, apiSettings);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?children.friends.name=Castillo");

            var result = controller.GetItems("families") as OkObjectResult;
            Assert.Equal(2, ((IEnumerable<dynamic>)result.Value).Count());

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void GetNested_ParentsSingleWork()
        {
            var filePath = UTHelpers.Up();
            var ds = new DataStore(filePath);
            var apiSettings = Options.Create(new ApiSettings());

            var controller = new DynamicController(ds, apiSettings);

            var result = controller.GetNested("families", "1", "parents/1/work") as OkObjectResult;
            Assert.Equal("APEXTRI", ((dynamic)result.Value).companyName);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void GetNested_ParentsSingle()
        {
            var filePath = UTHelpers.Up();
            var ds = new DataStore(filePath);
            var apiSettings = Options.Create(new ApiSettings());

            var controller = new DynamicController(ds, apiSettings);

            var result = controller.GetNested("families", "1", "parents/1") as OkObjectResult;
            Assert.Equal("Kim", ((dynamic)result.Value).name);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void GetNested_ParentsList()
        {
            var filePath = UTHelpers.Up();
            var ds = new DataStore(filePath);
            var apiSettings = Options.Create(new ApiSettings());

            var controller = new DynamicController(ds, apiSettings);

            var result = controller.GetNested("families", "1", "parents") as OkObjectResult;
            Assert.Equal(2, ((IEnumerable<dynamic>)result.Value).Count());

            UTHelpers.Down(filePath);
        }
    }
}