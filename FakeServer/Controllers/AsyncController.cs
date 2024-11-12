using FakeServer.Common;
using System.Dynamic;
using System.Net;
using FakeServer.Jobs;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FakeServer.Controllers;

[Authorize]
[Route(Config.AsyncRoute)]
public class AsyncController : Controller
{
    private readonly IDataStore _ds;
    private readonly JobsService _jobs;
    private readonly DataStoreSettings _dsSettings;

    public AsyncController(IDataStore ds, JobsService jobs, IOptions<DataStoreSettings> dsSettings)
    {
        _ds = ds;
        _jobs = jobs;
        _dsSettings = dsSettings.Value;
    }

    /// <summary>
    /// Add new item
    /// </summary>
    /// <param name="collectionId">Collection id</param>
    /// <param name="item">Item to add</param>
    /// <returns></returns>
    /// <response code="202">New async operation started</response>
    [HttpPost("{collectionId}")]
    public IActionResult AddNewItem(string collectionId, [FromBody] JToken item)
    {
        if (item == null)
            return BadRequest();

        var action = new Func<string>(() =>
        {
            var collection = _ds.GetCollection(collectionId);
            collection.InsertOne(item);
            return item[_dsSettings.IdField].Value<string>();
        });

        var queueUrl = _jobs.StartNewJob(collectionId, "POST", action);

        // TODO: Cancel & delete
        return new AcceptedResult($"{Request.Scheme}://{Request.Host.Value}/{queueUrl}", null);
    }

    /// <summary>
    /// Replace item from collection
    /// </summary>
    /// <param name="collectionId">Collection id</param>
    /// <param name="id">Id of the item to be replaced</param>
    /// <param name="item">Item's new content</param>
    /// <returns></returns>
    /// <response code="202">New async operation started</response>
    [HttpPut("{collectionId}/{id}")]
    public IActionResult ReplaceItem(string collectionId, [FromRoute][DynamicBinder] dynamic id, [FromBody] dynamic item)
    {
        if (item == null)
            return BadRequest();

        ObjectHelper.SetFieldValue(item, _dsSettings.IdField, id);

        var action = new Func<dynamic>(() =>
        {
            _ds.GetCollection(collectionId).ReplaceOne(id, item);
            return id;
        });

        var queueUrl = _jobs.StartNewJob(collectionId, "PUT", action);
        return new AcceptedResult($"{Request.Scheme}://{Request.Host.Value}/{queueUrl}", null);
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
    /// <response code="202">New async operation started</response>
    /// <response code="404">Item not found</response>
    /// <response code="415">Unsupported content type</response>
    [HttpPatch("{collectionId}/{id}")]
    [Consumes(Constants.JsonMergePatch, new[] { Constants.MergePatchJson })]
    public IActionResult UpdateItemMerge(string collectionId, [FromRoute][DynamicBinder] dynamic id, [FromBody] JToken patchData)
    {
        dynamic sourceData = JsonConvert.DeserializeObject<ExpandoObject>(patchData.ToString());

        if (!((IDictionary<string, object>)sourceData).Any())
            return BadRequest();

        var item = _ds.GetCollection(collectionId).Find(e => ObjectHelper.CompareFieldValueWithId(e, _dsSettings.IdField, id)).FirstOrDefault();

        if (item == null)
            return NotFound();

        var action = new Func<dynamic>(() =>
        {
            _ds.GetCollection(collectionId).UpdateOne(id, sourceData);
            return id;
        });

        var queueUrl = _jobs.StartNewJob(collectionId, "PATCH", action);
        return new AcceptedResult($"{Request.Scheme}://{Request.Host.Value}/{queueUrl}", null);
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
    /// <param name="collectionId"></param>
    /// <param name="itemId">Id of the item to be updated</param>
    /// <param name="patchDoc">Patch document</param>
    /// <returns></returns>
    /// <response code="202">New async operation started</response>
    /// <response code="404">Item not found</response>
    /// <response code="415">Unsupported content type</response>
    [HttpPatch("{collectionId}/{itemId}")]
    [Consumes(Constants.JsonPatchJson)]
    public IActionResult UpdateItemJsonPatch(string collectionId, dynamic itemId, [FromBody] JsonPatchDocument patchDoc)
    {
        if (_ds.IsItem(collectionId))
            return BadRequest();

        var item = _ds.GetCollection(collectionId).AsQueryable().FirstOrDefault(e => ObjectHelper.CompareFieldValueWithId(e, _dsSettings.IdField, itemId));

        if (item == null)
            return NotFound();

        var action = new Func<dynamic>(() =>
        {
            patchDoc.ApplyTo(item);
            _ds.GetCollection(collectionId).UpdateOne(itemId, item);
            return itemId;
        });

        var queueUrl = _jobs.StartNewJob(collectionId, "PATCH", action);
        return new AcceptedResult($"{Request.Scheme}://{Request.Host.Value}/{queueUrl}", null);
    }

    /// <summary>
    /// Remove item from collection
    /// </summary>
    /// <param name="collectionId">Collection id</param>
    /// <param name="id">Id of the item to be removed</param>
    /// <returns></returns>
    /// <response code="202">New async operation started</response>
    [HttpDelete("{collectionId}/{id}")]
    public IActionResult DeleteItem(string collectionId, [FromRoute][DynamicBinder] dynamic id)
    {
        var action = new Func<dynamic>(() =>
        {
            _ds.GetCollection(collectionId).DeleteOne(id);
            return id;
        });

        var queueUrl = _jobs.StartNewJob(collectionId, "DELETE", action);
        return new AcceptedResult($"{Request.Scheme}://{Request.Host.Value}/{queueUrl}", null);
    }

    /// <summary>
    /// Get the job status
    /// </summary>
    /// <param name="queueId">Job's queue Id</param>
    /// <returns></returns>
    /// <response code="200">Job is not completed</response>
    /// <response code="303">Job is completed</response>
    /// <response code="404">Item not found</response>
    [HttpGet("queue/{queueId}")]
    public IActionResult GetQueueItem(string queueId)
    {
        var job = _jobs.GetJob(queueId);

        if (job == null)
            return new NotFoundResult();

        if (!job.Action.IsCompleted)
            return new OkResult();

        Response.StatusCode = (int)HttpStatusCode.SeeOther;
        Response.Headers.Add("Location", new StringValues($"{Request.Scheme}://{Request.Host.Value}/api/{job.Collection}/{job.Action.Result}"));
        return new ContentResult();
    }

    /// <summary>
    /// Delete the job item from the queue
    /// </summary>
    /// <param name="queueId">Job's queue Id</param>
    /// <returns></returns>
    /// <response code="204">Item found and deleted</response>
    /// <response code="404">Item not found</response>
    [HttpDelete("queue/{queueId}")]
    public IActionResult DeleteQueueItem(string queueId)
    {
        return _jobs.DeleteJob(queueId) ? NoContent() : NotFound();
    }
}