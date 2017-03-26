using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
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

        // GET api/
        [HttpGet]
        public IEnumerable<string> GetCollections()
        {
            return _ds.ListCollections();
        }

        // POST api/
        [HttpPost]
        public void UpdateAllData([FromBody]string value)
        {
            _ds.UpdateAll(value);
        }

        // GET api/user
        // GET api/user?some=value&other=value
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

        // GET api/user/1
        [HttpGet("{collectionId}/{id}")]
        public IActionResult GetItem(string collectionId, int id)
        {
            var result = _ds.GetCollection(collectionId).Find(e => e.id == id).FirstOrDefault();

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST api/user
        [HttpPost("{collectionId}")]
        public async Task<IActionResult> AddNewItem(string collectionId, [FromBody]dynamic value)
        {
            await _ds.GetCollection(collectionId).InsertOneAsync(value);
            return Ok();
        }

        // PUT api/user/5
        [HttpPut("{collectionId}/{id}")]
        public async Task<IActionResult> ReplaceItem(string collectionId, int id, [FromBody]dynamic value)
        {
            await _ds.GetCollection(collectionId).ReplaceOneAsync((Predicate<dynamic>)(e => e.id == id), value);
            return Ok();
        }

        // PATCH api/user/5
        [HttpPatch("{collectionId}/{id}")]
        public async Task<IActionResult> UpdateItem(string collectionId, int id, [FromBody]dynamic value)
        {
            dynamic sourceData = JsonConvert.DeserializeObject<ExpandoObject>(value.ToString());

            await _ds.GetCollection(collectionId).UpdateOneAsync((Predicate<dynamic>)(e => e.id == id), sourceData);
            return Ok();
        }

        public static dynamic GetDynamicObject(Dictionary<string, object> properties)
        {
            var dynamicObject = new ExpandoObject() as IDictionary<string, Object>;
            foreach (var property in properties)
            {
                dynamicObject.Add(property.Key, property.Value);
            }
            return dynamicObject;
        }

        // DELETE api/user/5
        [HttpDelete("{collectionId}/{id}")]
        public async Task<IActionResult> DeleteItem(string collectionId, int id)
        {
            await _ds.GetCollection(collectionId).DeleteOneAsync(e => e.id == id);
            return Ok();
        }

        private bool Equals(dynamic x, string propName, dynamic value)
        {
            var val = ((IDictionary<string, object>)x)[propName];
            return ((dynamic)val).ToString() == value.ToString();
        }
    }
}