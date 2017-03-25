using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CSharp.RuntimeBinder;
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
        public IActionResult GetCollection(string collectionId)
        {
            var data = _ds.GetCollection(collectionId).AsQueryable();

            if (Request.Query.Keys.Count == 0)
            {
                return Ok(data);
            }

            // TODO: How to build Expressions with dynamics? Expressions.Dynamic?

            foreach (var key in Request.Query.Keys)
            {
                data = data.Where(d => Equals(d as ExpandoObject, key, Request.Query[key]));
            }

            return Ok(data);
        }

        // GET api/user/1
        [HttpGet("{collectionId}/{id}")]
        public IActionResult GetEntity(string collectionId, int id)
        {
            var result = _ds.GetCollection(collectionId).Find(e => e.id == id).FirstOrDefault();

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST api/user
        [HttpPost("{collectionId}")]
        public async Task<IActionResult> AddNewEntity(string collectionId, [FromBody]dynamic value)
        {
            await _ds.GetCollection(collectionId).InsertOneAsync(value);
            return Ok();
        }

        // PUT api/user/5
        [HttpPut("{collectionId}/{id}")]
        public async Task<IActionResult> ReplaceEntity(string collectionId, int id, [FromBody]dynamic value)
        {
            await _ds.GetCollection(collectionId).ReplaceOneAsync((Predicate<dynamic>)(e => e.id == id), value);
            return Ok();
        }

        // DELETE api/user/5
        [HttpDelete("{collectionId}/{id}")]
        public async Task<IActionResult> DeleteEntity(string collectionId, int id)
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