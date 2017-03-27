using JsonFlatFileDataStore;
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
    [Route("api")]
    public class DynamicController : Controller
    {
        private readonly DataStore _ds;

        public DynamicController(DataStore ds)
        {
            _ds = ds;
        }

        [HttpGet]
        public IEnumerable<string> GetCollections()
        {
            return _ds.ListCollections();
        }

        [HttpPost]
        public void UpdateAllData([FromBody]string value)
        {
            _ds.UpdateAll(value);
        }

        [HttpGet("{collectionId}")]
        public IActionResult GetItems(string collectionId, int skip = 0, int take = 10)
        {
            var data = _ds.GetCollection(collectionId).AsQueryable();

            var queryParams = Request.Query.Keys.ToList();
            queryParams.Remove("skip");
            queryParams.Remove("take");

            if (queryParams.Count == 0)
            {
                return Ok(data.Skip(skip).Take(take));
            }

            // TODO: How to build Expressions with dynamics? Expressions.Dynamic?

            foreach (var key in queryParams)
            {
                data = data.Where(d => Equals(d as ExpandoObject, key, Request.Query[key]));
            }

            return Ok(data.Skip(skip).Take(take));
        }

        [HttpGet("{collectionId}/{id}")]
        public IActionResult GetItem(string collectionId, int id)
        {
            var result = _ds.GetCollection(collectionId).Find(e => e.id == id).FirstOrDefault();

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("{collectionId}")]
        public async Task<IActionResult> AddNewItem(string collectionId, [FromBody]JToken value)
        {
            var collection = _ds.GetCollection(collectionId);

            dynamic itemToInsert = JsonConvert.DeserializeObject<ExpandoObject>(value.ToString());
            itemToInsert.id = collection.GetNextIdValue();

            await collection.InsertOneAsync(itemToInsert);
            return Ok(new { id = itemToInsert.id });
        }

        [HttpPut("{collectionId}/{id}")]
        public async Task<IActionResult> ReplaceItem(string collectionId, int id, [FromBody]dynamic value)
        {
            // Make sure that new data has id field correctly
            value.id = id;

            var success = await _ds.GetCollection(collectionId).ReplaceOneAsync((Predicate<dynamic>)(e => e.id == id), value);

            if (success)
                return Ok();
            else
                return NotFound();
        }

        [HttpPatch("{collectionId}/{id}")]
        public async Task<IActionResult> UpdateItem(string collectionId, int id, [FromBody]JToken value)
        {
            dynamic sourceData = JsonConvert.DeserializeObject<ExpandoObject>(value.ToString());

            if (!((IDictionary<string, Object>)sourceData).Any())
                return BadRequest();

            var success = await _ds.GetCollection(collectionId).UpdateOneAsync((Predicate<dynamic>)(e => e.id == id), sourceData);

            if (success)
                return Ok();
            else
                return NotFound();
        }

        [HttpDelete("{collectionId}/{id}")]
        public async Task<IActionResult> DeleteItem(string collectionId, int id)
        {
            var success = await _ds.GetCollection(collectionId).DeleteOneAsync(e => e.id == id);

            if (success)
                return Ok();
            else
                return NotFound();
        }

        private dynamic GetDynamicObject(Dictionary<string, object> properties)
        {
            var dynamicObject = new ExpandoObject() as IDictionary<string, object>;
            foreach (var property in properties)
            {
                dynamicObject.Add(property.Key, property.Value);
            }
            return dynamicObject;
        }

        private bool Equals(dynamic x, string propName, dynamic value)
        {
            var val = ((IDictionary<string, object>)x)[propName];
            return ((dynamic)val).ToString() == value.ToString();
        }
    }
}