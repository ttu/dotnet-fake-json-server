using FakeServer.Common;
using FakeServer.Jobs;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;

namespace FakeServer.Controllers
{
    [Authorize]
    [Route(Config.AsyncRoute)]
    public class AsyncController : Controller
    {
        private readonly IDataStore _ds;
        private readonly JobsService _jobs;

        public AsyncController(IDataStore ds, JobsService jobs)
        {
            _ds = ds;
            _jobs = jobs;
        }

        /// <summary>
        /// Add new item
        /// </summary>
        /// <param name="collectionId">Collection id</param>
        /// <param name="item">Item to add</param>
        /// <returns></returns>
        [HttpPost("{collectionId}")]
        public IActionResult AddNewItem(string collectionId, [FromBody] JToken item)
        {
            if (item == null)
                return BadRequest();

            var action = new Func<string>(() =>
            {
                var collection = _ds.GetCollection(collectionId);
                collection.InsertOneAsync(item);
                return item["id"].Value<string>();
            });

            var queuUrl = _jobs.StartNewJob(collectionId, "POST", action);

            // TODO: Cancel & delete
            return new AcceptedResult($"{Request.Scheme}://{Request.Host.Value}/{queuUrl}", null);
        }

        /// <summary>
        /// Replace item from collection
        /// </summary>
        /// <param name="collectionId">Collection id</param>
        /// <param name="id">Id of the item to be replaced</param>
        /// <param name="item">Item's new content</param>
        /// <returns></returns>
        [HttpPut("{collectionId}/{id}")]
        public IActionResult ReplaceItem(string collectionId, int id, [FromBody]dynamic item)
        {
            if (item == null)
                return BadRequest();

            item.id = id;

            var action = new Func<dynamic>(() =>
            {
                _ds.GetCollection(collectionId).ReplaceOne((Predicate<dynamic>)(e => e.id == id), item);
                return id;
            });

            var queuUrl = _jobs.StartNewJob(collectionId, "PUT", action);
            return new AcceptedResult($"{Request.Scheme}://{Request.Host.Value}/{queuUrl}", null);
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
        [HttpPatch("{collectionId}/{id}")]
        public IActionResult UpdateItem(string collectionId, int id, [FromBody]JToken patchData)
        {
            dynamic sourceData = JsonConvert.DeserializeObject<ExpandoObject>(patchData.ToString());

            if (!((IDictionary<string, Object>)sourceData).Any())
                return BadRequest();

            var action = new Func<dynamic>(() =>
            {
                _ds.GetCollection(collectionId).UpdateOne((Predicate<dynamic>)(e => e.id == id), sourceData);
                return id;
            });

            var queuUrl = _jobs.StartNewJob(collectionId, "PATCH", action);
            return new AcceptedResult($"{Request.Scheme}://{Request.Host.Value}/{queuUrl}", null);
        }

        /// <summary>
        /// Remove item from collection
        /// </summary>
        /// <param name="collectionId">Collection id</param>
        /// <param name="id">Id of the item to be removed</param>
        /// <returns></returns>
        [HttpDelete("{collectionId}/{id}")]
        public IActionResult DeleteItem(string collectionId, int id)
        {
            var action = new Func<dynamic>(() =>
            {
                var found = _ds.GetCollection(collectionId).DeleteOne(e => e.id == id);
                return id;
            });

            var queuUrl = _jobs.StartNewJob(collectionId, "DELETE", action);
            return new AcceptedResult($"{Request.Scheme}://{Request.Host.Value}/{queuUrl}", null);
        }

        /// <summary>
        /// Get job from queue
        /// </summary>
        /// <param name="queueId">Job's queue Id</param>
        /// <returns></returns>
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

        [HttpDelete("queue/{queueId}")]
        public IActionResult DeleteQueueItem(string queueId)
        {
            if (_jobs.DeleteJob(queueId))
                return Ok();
            else
                return NotFound();
        }
    }
}