using FakeServer.Common;
using FakeServer.WebSockets;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FakeServer.GraphQL
{
    public class GraphQLMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDataStore _datastore;
        private readonly IMessageBus _bus;
        private readonly bool _authenticationEnabled;
        private readonly string _idFieldName;
        private readonly string[] _allowedTypes = new[] { "application/graphql", "application/json" };
        private readonly string[] _allowedMethods = new[] { HttpMethods.Get, HttpMethods.Post };
        
        public GraphQLMiddleware(RequestDelegate next, IDataStore datastore, IMessageBus bus, bool authenticationEnabled, string idFieldName)
        {
            _next = next;
            _datastore = datastore;
            _bus = bus;
            _authenticationEnabled = authenticationEnabled;
            _idFieldName = idFieldName;
        }

        public async Task Invoke(HttpContext context)
        {
            // POST application/graphql body is query
            // POST application/json and { "query": "..." }
            // POST /graphql?query={users{name}}
            // GET /graphql?query={users{name}}

            if (!context.Request.Path.Value.StartsWith($"/{Config.GraphQLRoute}"))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            if (_authenticationEnabled && !context.User.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            if (!_allowedMethods.Any(context.Request.Method.Contains) ||
            (context.Request.Method == HttpMethods.Post && !_allowedTypes.Any(context.Request.ContentType.Contains))
            )
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { errors = new[] { "Not implemented" } }));
                return;
            }

            GraphQLResult result = null;

            var (success, query, error) = await ParseQuery(context);

            if (!success)
            {
                result = new GraphQLResult { Errors = new List<string> { error } };
            }
            else
            {
                var toReplace = new[] { "\r\n", "\\r\\n", "\\n", "\n" };

                query = toReplace.Aggregate(query, (acc, curr) => acc.Replace(curr, ""));

                result = GraphQL.HandleQuery(query, _datastore, _idFieldName);
            }

            var json = result.Errors?.Any() == true
                            ? JsonConvert.SerializeObject(new { data = result.Data, errors = result.Errors })
                            : JsonConvert.SerializeObject(new { data = result.Data });

            result.Notifications?.ForEach(msg => _bus.Publish("updated", msg));

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = result.Errors?.Any() == true ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.OK;

            await context.Response.WriteAsync(json);
        }

        private static async Task<(bool success, string body, string error)> ParseQuery(HttpContext context)
        {
            // If the "query" query string parameter exists, we don't care about the body or the request type
            if (context.Request.Query.TryGetValue("query", out StringValues query))
            {
                return (true, query[0], null);
            }
            else if (context.Request.Method == HttpMethods.Get)
            {
                return (false, null, "Missing query parameter `query`");
            }

            string body;

            using (var streamReader = new StreamReader(context.Request.Body))
            {
                body = await streamReader.ReadToEndAsync().ConfigureAwait(true);
            }

            if (context.Request.ContentType.StartsWith("application/graphql"))
            {
                return (true, body, null);
            }

            dynamic jsonBody;

            try
            {
                jsonBody = JsonConvert.DeserializeObject(body);
            }
            catch (Exception e)
            {
                return (false, null, e.Message);
            }

            if (jsonBody.query is null)
            {
                return (false, null, "Missing query property in json.");
            }

            return (true, jsonBody.query, null);
        }
    }
}