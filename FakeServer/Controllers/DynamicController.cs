using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace FakeServer.Controllers
{
    [Authorize]
    [Route("api")]
    public class DynamicController : Controller
    {
        private readonly IDataStore _ds;

        public DynamicController(IDataStore ds)
        {
            _ds = ds;
        }

        /// <summary>
        /// List collections
        /// </summary>
        /// <returns>List of collections</returns>
        [HttpGet]
        public IEnumerable<string> GetCollections()
        {
            return _ds.ListCollections();
        }

        /// <summary>
        /// Replace database.json content
        /// </summary>
        /// <param name="value">New JSON content</param>
        [HttpPost]
        public void UpdateAllData([FromBody]string value)
        {
            _ds.UpdateAll(value);
        }

        /// <summary>
        /// Get items
        /// </summary>
        /// <remarks>
        /// Add filtering with query parameters. E.q. /api/user?age=22&amp;name=Phil (not possible with Swagger)
        /// </remarks>
        /// <param name="collectionId">Collection id</param>
        /// <param name="skip">Items to skip</param>
        /// <param name="take">Items to take</param>
        /// <returns>List of items</returns>
        /// <response code="200">Collection item array</response>
        /// <response code="404">Collection not found</response>
        [HttpGet("{collectionId}")]
        public IActionResult GetItems(string collectionId, int skip = 0, int take = 10)
        {
            var datas = _ds.GetCollection(collectionId).AsQueryable();

            // Collection can actually just be empty, but in this case we handle it as it is not found
            if (!datas.Any())
                return NotFound();

            var queryParams = Request.Query.Keys.ToList();
            queryParams.Remove("skip");
            queryParams.Remove("take");

            if (queryParams.Count == 0)
            {
                return Ok(datas.Skip(skip).Take(take));
            }

            foreach (var key in queryParams)
            {
                datas = datas.Where(d => ObjectHelper.GetPropertyAndCompare(d as ExpandoObject, key, Request.Query[key]));
            }

            return Ok(datas.Skip(skip).Take(take));
        }

        /// <summary>
        /// Get single item
        /// </summary>
        /// <param name="collectionId">Collection id</param>
        /// <param name="id">Item id</param>
        /// <returns>Item</returns>
        /// <response code="200">Item found</response>
        /// <response code="404">Item not found</response>
        [HttpGet("{collectionId}/{id}")]
        public IActionResult GetItem(string collectionId, int id)
        {
            var result = _ds.GetCollection(collectionId).Find(e => e.id == id).FirstOrDefault();

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Get nested item
        /// </summary>
        /// <remarks>
        /// Add full path separated with periods. E.q. /api/user/1/parents/0/work
        /// </remarks>
        /// <param name="collectionId">Collection id</param>
        /// <param name="id">Item id</param>
        /// <param name="path">Rest of the path</param>
        /// <returns></returns>
        /// <response code="200">Nested item found</response>
        /// <response code="400">Parent item not found</response>
        /// <response code="404">Nested item not found</response>
        [HttpGet("{collectionId}/{id}/{*path}")]
        public IActionResult GetNested(string collectionId, int id, string path)
        {
            var routes = path.Split('/');

            var item = _ds.GetCollection(collectionId).AsQueryable().FirstOrDefault(e => e.id == id);

            if (item == null)
                return BadRequest();

            var nested = ObjectHelper.GetNestedProperty(item, path);

            if (nested == null)
                return NotFound();

            return Ok(nested);
        }

        /// <summary>
        /// Add new item
        /// </summary>
        /// <param name="collectionId">Collection id</param>
        /// <param name="item">Item to add</param>
        /// <returns>Created item id</returns>
        /// <response code="201">Item created</response>
        /// <response code="400">Item is null</response>
        [HttpPost("{collectionId}")]
        public async Task<IActionResult> AddNewItem(string collectionId, [FromBody]JToken item)
        {
            if (item == null)
                return BadRequest();

            var collection = _ds.GetCollection(collectionId);
            
            await collection.InsertOneAsync(item);

            return Created($"/api/{collectionId}/{item["id"]}", new { id = item["id"] });
        }

        /// <summary>
        /// Replace item from collection
        /// </summary>
        /// <param name="collectionId">Collection id</param>
        /// <param name="id">Id of the item to be replaced</param>
        /// <param name="item">Item's new content</param>
        /// <returns></returns>
        /// <response code="200">Item found and replaced</response>
        /// <response code="400">Item is null</response>
        /// <response code="404">Item not found</response>
        [HttpPut("{collectionId}/{id}")]
        public async Task<IActionResult> ReplaceItem(string collectionId, int id, [FromBody]dynamic item)
        {
            if (item == null)
                return BadRequest();

            // Make sure that new data has id field correctly
            item.id = id;

            var success = await _ds.GetCollection(collectionId).ReplaceOneAsync((Predicate<dynamic>)(e => e.id == id), item);

            if (success)
                return Ok();
            else
                return NotFound();
        }

        /// <summary>
        /// Update item's content
        /// </summary>
        /// <remarks>
        /// Patch data contains fields to be updated.
        ///
        ///     {
        ///        "stringField": "some value",
        ///        "intField": 22,
        ///        "boolField": true
        ///     }
        /// </remarks>
        /// <param name="collectionId"></param>
        /// <param name="id">Id of the item to be updated</param>
        /// <param name="patchData">Patch data</param>
        /// <returns></returns>
        /// <response code="200">Item found and updated</response>
        /// <response code="400">Patch data is empty</response>
        /// <response code="404">Item not found</response>
        [HttpPatch("{collectionId}/{id}")]
        public async Task<IActionResult> UpdateItem(string collectionId, int id, [FromBody]JToken patchData)
        {
            dynamic sourceData = JsonConvert.DeserializeObject<ExpandoObject>(patchData.ToString());

            if (!((IDictionary<string, Object>)sourceData).Any())
                return BadRequest();

            var success = await _ds.GetCollection(collectionId).UpdateOneAsync((Predicate<dynamic>)(e => e.id == id), sourceData);

            if (success)
                return Ok();
            else
                return NotFound();
        }

        /// <summary>
        /// Remove item from collection
        /// </summary>
        /// <param name="collectionId">Collection id</param>
        /// <param name="id">Id of the item to be removed</param>
        /// <returns></returns>
        /// <response code="200">Item found and removed</response>
        /// <response code="404">Item not found</response>
        [HttpDelete("{collectionId}/{id}")]
        public async Task<IActionResult> DeleteItem(string collectionId, int id)
        {
            var success = await _ds.GetCollection(collectionId).DeleteOneAsync(e => e.id == id);

            if (success)
                return Ok();
            else
                return NotFound();
        }        
    }
}