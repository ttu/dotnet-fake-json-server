using System.Dynamic;
using FakeServer.Common;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FakeServer.Controllers;

[Authorize]
[Route(Config.ApiRoute)]
public class DynamicController : Controller
{
    private readonly IDataStore _ds;
    private readonly ApiSettings _apiSettings;
    private readonly DataStoreSettings _dsSettings;

    public DynamicController(IDataStore ds, IOptions<ApiSettings> apiSettings, IOptions<DataStoreSettings> dsSettings)
    {
        _ds = ds;
        _apiSettings = apiSettings.Value;
        _dsSettings = dsSettings.Value;
    }

    /// <summary>
    /// List keys
    /// </summary>
    /// <returns>List of keys</returns>
    [HttpGet]
    [HttpHead]
    public IEnumerable<string> GetKeys()
    {
        return _ds.GetKeys().Select(e => e.Key);
    }

    /// <summary>
    /// Replace database.json content
    /// </summary>
    /// <param name="value">New JSON content</param>
    [HttpPost]
    public void UpdateAllData([FromBody] string value)
    {
        _ds.UpdateAll(value);
    }

    /// <summary>
    /// Get items
    /// </summary>
    /// <remarks>
    /// Add filtering with query parameters. E.q. /api/users?age=22&amp;name=Phil (not possible with Swagger)
    ///
    /// Optional parameter names skip/take and offset/limit and page/per_page:
    /// /api/users?skip=10&amp;take=20
    /// /api/users?offset=10&amp;limit=20
    /// /api/users?page=2&amp;per_page=20
    ///  </remarks>
    /// <param name="collectionId">Collection id</param>
    /// <param name="skip">Items to skip (optional name offset)</param>
    /// <param name="take">Items to take (optional name limit)</param>
    /// <returns>List of items</returns>
    /// <response code="200">Collection item array</response>
    /// <response code="400">Invalid query parameters</response>
    /// <response code="404">Collection not found</response>
    [HttpGet("{collectionId}")]
    [HttpHead("{collectionId}")]
    public IActionResult GetItems(string collectionId, int skip = 0, int take = 512)
    {
        var found = _ds.GetKeys().TryGetValue(collectionId, out var itemType);

        if (found == false)
            return NotFound();

        return itemType == JsonFlatFileDataStore.ValueType.Item ? GetSingleItem(collectionId) : GetCollectionItem(collectionId, skip, take);
    }

    private IActionResult GetSingleItem(string itemId)
    {
        var item = _ds.GetItem(itemId);
        return item != null ? Ok(item) : NotFound();
    }

    private IActionResult GetCollectionItem(string collectionId, int skip, int take)
    {
        if (!QueryHelper.IsQueryValid(Request.Query))
            return BadRequest();

        var options = QueryHelper.GetQueryOptions(Request.Query, skip, take);

        if (!options.Validate())
            return BadRequest();

        var collection = _ds.GetCollection(collectionId);

        var datas = options.IsTextSearch ? collection.Find(Request.Query["q"]) : collection.AsQueryable();

        foreach (var key in options.QueryParams)
        {
            var propertyName = key;
            var compareFunc = ObjectHelper.Funcs[""];

            var idx = key.LastIndexOf("_");

            if (idx != -1)
            {
                var op = key[idx..];
                compareFunc = ObjectHelper.Funcs[op];
                propertyName = key.Replace(op, "");
            }

            datas = datas.Where(d => ObjectHelper.GetPropertyAndCompare(d as ExpandoObject, propertyName, Request.Query[key], compareFunc));
        }

        var totalCount = datas.Count();

        var paginationHeader = QueryHelper.GetPaginationHeader($"{Request.Scheme}://{Request.Host.Value}{Request.Path}", totalCount, options.Skip, options.Take,
            options.SkipWord, options.TakeWord);

        var results = datas.Skip(options.Skip).Take(options.Take);

        if (options.Fields.Any())
        {
            results = ObjectHelper.SelectFields(results, options.Fields);
        }

        if (options.SortFields.Any())
        {
            results = SortHelper.SortFields(results, options.SortFields);
        }

        if (_apiSettings.UseResultObject)
        {
            return Ok(QueryHelper.GetResultObject(results, totalCount, paginationHeader, options));
        }

        Response.Headers.Add("X-Total-Count", totalCount.ToString());
        Response.Headers.Add("Link", QueryHelper.GetHeaderLink(paginationHeader));
        return Ok(results);
    }

    /// <summary>
    /// Get single item
    /// </summary>
    /// <param name="collectionId">Collection id</param>
    /// <param name="id">Item id</param>
    /// <returns>Item</returns>
    /// <response code="200">Item found</response>
    /// <response code="400">Item is not collection</response>
    /// <response code="404">Item not found</response>
    [HttpGet("{collectionId}/{id}")]
    [HttpHead("{collectionId}/{id}")]
    public IActionResult GetItem(string collectionId, [FromRoute] [DynamicBinder] dynamic id)
    {
        if (_ds.IsItem(collectionId))
            return BadRequest();

        var result = _ds.GetCollection(collectionId).Find(e => ObjectHelper.CompareFieldValueWithId(e, _dsSettings.IdField, id)).FirstOrDefault();

        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Get nested item
    /// </summary>
    /// <remarks>
    /// Add full path separated with periods. E.q. /api/users/1/parents/0/work
    /// </remarks>
    /// <param name="collectionId">Collection id</param>
    /// <param name="id">Item id</param>
    /// <param name="path">Rest of the path</param>
    /// <returns></returns>
    /// <response code="200">Nested item found</response>
    /// <response code="400">Parent item not found or item not collection</response>
    /// <response code="404">Nested item not found</response>
    [HttpGet("{collectionId}/{id}/{*path}")]
    [HttpHead("{collectionId}/{id}/{*path}")]
    public IActionResult GetNested(string collectionId, [FromRoute] [DynamicBinder] dynamic id, string path)
    {
        if (_ds.IsItem(collectionId))
            return BadRequest();

        var item = _ds.GetCollection(collectionId).AsQueryable().FirstOrDefault(e => ObjectHelper.CompareFieldValueWithId(e, _dsSettings.IdField, id));

        if (item == null)
            return BadRequest();

        var nested = ObjectHelper.GetNestedProperty(item, path, _dsSettings.IdField);

        return nested != null ? Ok(nested) : NotFound();
    }

    /// <summary>
    /// Add new item
    /// </summary>
    /// <param name="collectionId">Collection id</param>
    /// <param name="item">Item to add</param>
    /// <returns>Created item id</returns>
    /// <response code="201">Item created</response>
    /// <response code="400">Item is null</response>
    /// <response code="409">Collection is an object</response>
    [HttpPost("{collectionId}")]
    public async Task<IActionResult> AddNewItem(string collectionId, [FromBody] JToken item)
    {
        if (item == null)
            return BadRequest();

        if (_ds.IsItem(collectionId))
            return Conflict();

        var collection = _ds.GetCollection(collectionId);

        await collection.InsertOneAsync(item);

        var createdItem = new ExpandoObject();
        createdItem.TryAdd(_dsSettings.IdField, item[_dsSettings.IdField]);

        return Created($"{Request.GetDisplayUrl()}/{item[_dsSettings.IdField]}", createdItem);
    }

    /// <summary>
    /// Replace item from collection
    /// </summary>
    /// <param name="collectionId">Collection id</param>
    /// <param name="id">Id of the item to be replaced</param>
    /// <param name="item">Item's new content</param>
    /// <returns></returns>
    /// <response code="204">Item found and replaced</response>
    /// <response code="400">Replace data is null or item is not in a collection</response>
    /// <response code="404">Item not found</response>
    [HttpPut("{collectionId}/{id}")]
    public async Task<IActionResult> ReplaceItem(string collectionId, [FromRoute] [DynamicBinder] dynamic id, [FromBody] dynamic item)
    {
        if (item == null || _ds.IsItem(collectionId))
            return BadRequest();

        // Make sure that new data has id field correctly
        ObjectHelper.SetFieldValue(item, _dsSettings.IdField, id);

        var success = await _ds.GetCollection(collectionId).ReplaceOneAsync(id, item, _apiSettings.UpsertOnPut);

        return success ? NoContent() : NotFound();
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
    /// <param name="collectionId">Collection id</param>
    /// <param name="id">Id of the item to be updated</param>
    /// <param name="patchData">Patch data</param>
    /// <returns></returns>
    /// <response code="204">Item found and updated</response>
    /// <response code="400">Patch data is empty or item is not in a collection</response>
    /// <response code="404">Item not found</response>
    /// <response code="415">Unsupported content type</response>
    [HttpPatch("{collectionId}/{id}")]
    [Consumes(Constants.JsonMergePatch, new[] { Constants.MergePatchJson })]
    public async Task<IActionResult> UpdateItemMerge(string collectionId, [FromRoute] [DynamicBinder] dynamic id, [FromBody] JToken patchData)
    {
        dynamic sourceData = JsonConvert.DeserializeObject<ExpandoObject>(patchData.ToString());

        if (!((IDictionary<string, object>)sourceData).Any() || _ds.IsItem(collectionId))
            return BadRequest();

        var success = await _ds.GetCollection(collectionId).UpdateOneAsync(id, sourceData);

        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Update item's content
    /// </summary>
    /// <remarks>
    /// Patch document contains fields to be updated.
    ///
    ///     [
    ///        { "op": "test", "path": "/a/b/c", "value": "foo" },
    ///        { "op": "remove", "path": "/a/b/c" },
    ///        { "op": "add", "path": "/a/b/c", "value": [ "foo", "bar" ] },
    ///        { "op": "replace", "path": "/a/b/c", "value": 42 },
    ///        { "op": "move", "from": "/a/b/c", "path": "/a/b/d" },
    ///        { "op": "copy", "from": "/a/b/d", "path": "/a/b/e" }
    ///     ]
    /// </remarks>
    /// <param name="collectionId">Collection id</param>
    /// <param name="itemId">Id of the item to be updated</param>
    /// <param name="patchDoc">Patch document</param>
    /// <returns></returns>
    /// <response code="204">Item found and updated</response>
    /// <response code="400">Patch data is empty or item is not in a collection</response>
    /// <response code="404">Item not found</response>
    /// <response code="415">Unsupported content type</response>
    [HttpPatch("{collectionId}/{itemId}")]
    [Consumes(Constants.JsonPatchJson)]
    public async Task<IActionResult> UpdateItemJsonPatch(string collectionId, [FromRoute] [DynamicBinder] dynamic itemId, [FromBody] JsonPatchDocument patchDoc)
    {
        if (_ds.IsItem(collectionId))
            return BadRequest();

        var item = _ds.GetCollection(collectionId).AsQueryable().FirstOrDefault(e => ObjectHelper.CompareFieldValueWithId(e, _dsSettings.IdField, itemId));

        if (item == null)
            return NotFound();

        if (patchDoc == null)
            return BadRequest();

        patchDoc.ApplyTo(item);

        var success = await _ds.GetCollection(collectionId).UpdateOneAsync(itemId, item);

        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Update Nested item's content
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
    /// <param name="collectionId">Collection id</param>
    /// <param name="id">Id of the item to be updated</param>
    /// <param name="path">Rest of the path</param>
    /// <param name="patchData">Patch data</param>
    /// <returns></returns>
    /// <response code="204">Item found and updated</response>
    /// <response code="400">Patch data is empty or item is not in a collection</response>
    /// <response code="404">Item not found</response>
    /// <response code="415">Unsupported content type</response>
    [HttpPatch("{collectionId}/{id}/{*path}")]
    [Consumes(Constants.JsonMergePatch, new[] { Constants.MergePatchJson })]
    public async Task<IActionResult> UpdateNestedItemMerge(string collectionId, [FromRoute] [DynamicBinder] dynamic id, string path,
        [FromBody] JToken patchData)
    {
        if (_ds.IsItem(collectionId))
            return BadRequest();

        var item = _ds.GetCollection(collectionId).AsQueryable().FirstOrDefault(e => ObjectHelper.CompareFieldValueWithId(e, _dsSettings.IdField, id));

        if (item == null)
            return NotFound();

        var nested = ObjectHelper.GetNestedProperty(item, Uri.UnescapeDataString(path), _dsSettings.IdField);

        if (nested == null)
            return NotFound();

        var sourceData = JsonConvert.DeserializeObject<ExpandoObject>(patchData.ToString());

        foreach (var kvp in sourceData)
        {
            ((IDictionary<string, object>)nested)[kvp.Key] = kvp.Value;
        }

        var success = await _ds.GetCollection(collectionId).UpdateOneAsync(id, item);

        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Update Nested item's content
    /// </summary>
    /// <remarks>
    /// Patch document contains fields to be updated.
    ///
    ///     [
    ///        { "op": "test", "path": "/a/b/c", "value": "foo" },
    ///        { "op": "remove", "path": "/a/b/c" },
    ///        { "op": "add", "path": "/a/b/c", "value": [ "foo", "bar" ] },
    ///        { "op": "replace", "path": "/a/b/c", "value": 42 },
    ///        { "op": "move", "from": "/a/b/c", "path": "/a/b/d" },
    ///        { "op": "copy", "from": "/a/b/d", "path": "/a/b/e" }
    ///     ]
    /// </remarks>
    /// <param name="collectionId">Collection id</param>
    /// <param name="itemId">Id of the item to be updated</param>
    /// <param name="path">Rest of the path</param>
    /// <param name="patchDoc">Patch document</param>
    /// <returns></returns>
    /// <response code="204">Item found and updated</response>
    /// <response code="400">Patch data is empty or item is not in a collection</response>
    /// <response code="404">Item not found</response>
    /// <response code="415">Unsupported content type</response>
    [HttpPatch("{collectionId}/{itemId}/{*path}")]
    [Consumes(Constants.JsonPatchJson)]
    public async Task<IActionResult> UpdateNestedItemJsonPatch(string collectionId, [FromRoute] [DynamicBinder] dynamic itemId, string path,
        [FromBody] JsonPatchDocument patchDoc)
    {
        if (_ds.IsItem(collectionId))
            return BadRequest();

        var item = _ds.GetCollection(collectionId).AsQueryable().FirstOrDefault(e => ObjectHelper.CompareFieldValueWithId(e, _dsSettings.IdField, itemId));

        if (item == null)
            return NotFound();

        var nested = ObjectHelper.GetNestedProperty(item, Uri.UnescapeDataString(path), _dsSettings.IdField);

        if (nested == null)
            return NotFound();

        if (patchDoc == null)
            return BadRequest();

        patchDoc.ApplyTo(nested);

        var success = await _ds.GetCollection(collectionId).UpdateOneAsync(itemId, item);

        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Remove item from collection
    /// </summary>
    /// <param name="collectionId">Collection id</param>
    /// <param name="id">Id of the item to be removed</param>
    /// <returns></returns>
    /// <response code="204">Item found and removed</response>
    /// <response code="400">Item is not in a collection</response>
    /// <response code="404">Item not found</response>
    [HttpDelete("{collectionId}/{id}")]
    public async Task<IActionResult> DeleteItem(string collectionId, [FromRoute] [DynamicBinder] dynamic id)
    {
        if (_ds.IsItem(collectionId))
            return BadRequest();

        var success = await _ds.GetCollection(collectionId).DeleteOneAsync(id);

        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Replace object
    /// </summary>
    /// <param name="objectId">Object id</param>
    /// <param name="item">Object's new content</param>
    /// <returns></returns>
    /// <response code="204">Object found and replaced</response>
    /// <response code="400">Replace data is null or item is in a collection</response>
    /// <response code="404">Object not found</response>
    [HttpPut("{objectId}")]
    public async Task<IActionResult> ReplaceSingleItem(string objectId, [FromBody] dynamic item)
    {
        if (_ds.IsCollection(objectId))
            return BadRequest();

        if (item == null)
            return BadRequest();

        var success = await _ds.ReplaceItemAsync(objectId, item, _apiSettings.UpsertOnPut);

        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Update single object's content
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
    /// <param name="objectId">Object id</param>
    /// <param name="patchData">Patch data</param>
    /// <returns></returns>
    /// <response code="204">Object found and updated</response>
    /// <response code="400">Patch data is empty</response>
    /// <response code="404">Object not found</response>
    /// <response code="415">Unsupported content type</response>
    [HttpPatch("{objectId}")]
    [Consumes(Constants.JsonMergePatch, new[] { Constants.MergePatchJson })]
    public async Task<IActionResult> UpdateSingleItemMerge(string objectId, [FromBody] JToken patchData)
    {
        dynamic sourceData = JsonConvert.DeserializeObject<ExpandoObject>(patchData.ToString());

        if (!((IDictionary<string, object>)sourceData).Any() || _ds.IsCollection(objectId))
            return BadRequest();

        var success = await _ds.UpdateItemAsync(objectId, sourceData);

        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Update single object's content
    /// </summary>
    /// <remarks>
    /// Patch document contains fields to be updated.
    ///
    ///     [
    ///        { "op": "test", "path": "/a/b/c", "value": "foo" },
    ///        { "op": "remove", "path": "/a/b/c" },
    ///        { "op": "add", "path": "/a/b/c", "value": [ "foo", "bar" ] },
    ///        { "op": "replace", "path": "/a/b/c", "value": 42 },
    ///        { "op": "move", "from": "/a/b/c", "path": "/a/b/d" },
    ///        { "op": "copy", "from": "/a/b/d", "path": "/a/b/e" }
    ///     ]
    /// </remarks>
    /// <param name="singleObjectId">Object id</param>
    /// <param name="patchDoc">Patch document</param>
    /// <returns></returns>
    /// <response code="204">Object found and updated</response>
    /// <response code="400">Patch data is empty</response>
    /// <response code="404">Object not found</response>
    /// <response code="415">Unsupported content type</response>
    [HttpPatch("{singleObjectId}")]
    [Consumes(Constants.JsonPatchJson)]
    public async Task<IActionResult> UpdateSingleItemJsonPatch(string singleObjectId, [FromBody] JsonPatchDocument patchDoc)
    {
        if (_ds.IsCollection(singleObjectId))
            return BadRequest();

        if (patchDoc == null)
            return BadRequest();

        var item = _ds.GetItem(singleObjectId);

        if (item == null)
            return NotFound();

        patchDoc.ApplyTo(item);

        var success = await _ds.UpdateItemAsync(singleObjectId, item);

        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Remove single object
    /// </summary>
    /// <param name="objectId">Single object id</param>
    /// <returns></returns>
    /// <response code="204">Object found and removed</response>
    /// <response code="400">Object is a collection</response>
    /// <response code="404">Object not found</response>
    [HttpDelete("{objectId}")]
    public async Task<IActionResult> DeleteSingleItem(string objectId)
    {
        if (_ds.IsCollection(objectId))
            return BadRequest();


        var success = await _ds.DeleteItemAsync(objectId);

        return success ? NoContent() : NotFound();
    }
}