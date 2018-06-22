using FakeServer.Common;
using FakeServer.WebSockets;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Http;
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

        public GraphQLMiddleware(RequestDelegate next, IDataStore datastore, IMessageBus bus, bool authenticationEnabled)
        {
            _next = next;
            _datastore = datastore;
            _bus = bus;
            _authenticationEnabled = authenticationEnabled;
        }

        public async Task Invoke(HttpContext context)
        {
            // POST application/graphql body is query
            // POST application/json and { "query": "..." }
            // TODO: POST /graphql?query={users{name}}
            // TODO: GET /graphql?query={users{name}}

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

            if (context.Request.Method != "POST" ||
                !context.Request.ContentType.ContainsAny("application/graphql", "application/json"))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { errors = new[] { "Not implemented" } }));
                return;
            }

            var result = default(GraphQLResult);
            try
            {
                string query = await ParseQuery(context);

                var toReplace = new[] { "\r\n", "\\r\\n", "\\n", "\n" };

                query = toReplace.Aggregate(query, (acc, curr) => acc.Replace(curr, ""));

                result = await GraphQL.HandleQuery(query, _datastore);
            }
            catch (Exception e)
            {
                if (result == default(GraphQLResult))
                {
                    result = new GraphQLResult() { Errors = new List<string>() };
                }
                else if (result.Errors == default(List<string>))
                {
                    result.Errors = new List<string>();
                }

                result.Errors.Add(e.Message);

            }

            var json = result.Errors?.Any() == true
                            ? JsonConvert.SerializeObject(new { data = result.Data, errors = result.Errors })
                            : JsonConvert.SerializeObject(new { data = result.Data });

            result.Notifications?.ForEach(msg => _bus.Publish("updated", msg));

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = result.Errors?.Any() == true ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.OK;

            await context.Response.WriteAsync(json);
        }

        private static async Task<string> ParseQuery(HttpContext context)
        {
            var body = string.Empty;

            using (var streamReader = new StreamReader(context.Request.Body))
            {
                body = await streamReader.ReadToEndAsync().ConfigureAwait(true);
            }

            if (context.Request.ContentType == "application/graphql")
            {
                return body;
            }

            dynamic jsonBody = JsonConvert.DeserializeObject(body);
            if (jsonBody.query is null)
            {
                throw new Exception("Required property 'query' not found in json body.");
            }

            return jsonBody.query;
        }
    }
}