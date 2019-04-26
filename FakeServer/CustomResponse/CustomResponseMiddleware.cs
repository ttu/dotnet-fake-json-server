using FakeServer.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FakeServer.CustomResponse
{
    // TODO: How to name these?
    public class Globals
    {
        public HttpContext _Context;
        public string _CollectionId;
        public string _Body;
    }

    public class CustomResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CustomResponseSettings _settings;
        private readonly Script<object> _script;

        public CustomResponseMiddleware(RequestDelegate next, IOptions<CustomResponseSettings> settings)
        {
            _next = next;
            _settings = settings.Value;
            _script = CSharpScript.Create<object>(_settings.Script,
                                                    ScriptOptions.Default
                                                                    .WithReferences(_settings.References)
                                                                    .WithImports(_settings.Usings),
                                                    globalsType: typeof(Globals));

            _script.Compile();
        }

        public async Task Invoke(HttpContext context)
        {
            if (!_settings.Methods.Contains(context.Request.Method) || !_settings.Paths.Any(context.Request.Path.Value.Contains))
            {
                await _next(context);
                return;
            }

            var originalStream = context.Response.Body;

            using (var ms = new MemoryStream())
            {
                context.Response.Body = ms;

                await _next(context);

                var bodyString = Encoding.UTF8.GetString((context.Response.Body as MemoryStream).ToArray());
                var globalObject = new Globals
                {
                    _Context = context,
                    _CollectionId = GetCollectionFromPath(context.Request.Path.Value),
                    _Body = RemoveLiterals(bodyString)
                };

                var script = await _script.RunAsync(globalObject);

                // HACK: Remove string quotemarks from around the _Body
                // Original body is e.g. in an array: [{\"id\":1,\"name\":\"Jame\s\"}]
                // Script will return new { Data = _Body }
                // Script will set Data as a string { Data = "[{\"id\":1,\"name\":\"Jame\s\"}]" }

                var jsonCleared = RemoveLiterals(JsonConvert.SerializeObject(script.ReturnValue));

                var bodyCleanStart = jsonCleared.IndexOf(globalObject._Body);
                var bodyCleanEnd = bodyCleanStart + globalObject._Body.Length;
                jsonCleared = jsonCleared
                                .Remove(bodyCleanEnd, 1)
                                .Remove(bodyCleanStart - 1, 1);

                var byteArray = Encoding.ASCII.GetBytes(jsonCleared);
                var stream = new MemoryStream(byteArray);
                await stream.CopyToAsync(originalStream);
            }
        }

        // TODO: Move to helper classes and add tests

        private string RemoveLiterals(string input) => Regex.Replace(input, "[\\\\](?=(\"))", "");

        private string GetCollectionFromPath(string path)
        {
            try
            {
                var collection = path.Remove(0, Config.ApiRoute.Length + 2);
                collection = collection.IndexOf("/") != -1 ? collection.Remove(collection.IndexOf("/")) : collection;
                collection = collection.IndexOf("?") != -1 ? collection.Remove(collection.IndexOf("?")) : collection;
                return collection;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}