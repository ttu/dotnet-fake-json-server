using FakeServer.Common;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
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
        private readonly bool _authenticationEnabled;

        public GraphQLMiddleware(RequestDelegate next, IDataStore datastore, bool authenticationEnabled)
        {
            _next = next;
            _datastore = datastore;
            _authenticationEnabled = authenticationEnabled;
        }

        public async Task Invoke(HttpContext context)
        {
            // POST application/graphql body is query
            // TODO: POST application/json and { "query": "..." }
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

            if (context.Request.Method != "POST" || !context.Request.ContentType.Contains("application/graphql"))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { errors = new[] { "Not implemented" } }));
                return;
            }

            var query = string.Empty;

            using (var streamReader = new StreamReader(context.Request.Body))
            {
                query = await streamReader.ReadToEndAsync().ConfigureAwait(true);
            }

            var toReplace = new[] { "\r\n", "\\r\\n", "\\n", "\n" };

            query = toReplace.Aggregate(query, (acc, curr) => acc.Replace(curr, ""));

            var result = await GraphQL.HandleQuery(query, _datastore);

            var json = result.Errors?.Any() == true
                            ? JsonConvert.SerializeObject(new { data = result.Data, errors = result.Errors })
                            : JsonConvert.SerializeObject(new { data = result.Data });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = result.Errors?.Any() == true ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.OK;

            await context.Response.WriteAsync(json);
        }
    }
}