using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FakeServer.Common.Formatters
{
    public class XmlOutputFormatter : TextOutputFormatter
    {
        public XmlOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanWriteType(Type type) =>
                typeof(ExpandoObject).IsAssignableFrom(type) || typeof(IEnumerable<object>).IsAssignableFrom(type) ? base.CanWriteType(type) : false;

        public async override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            XElement itemsToString(XElement acc, KeyValuePair<string, object> fields)
            {
                var element = fields.Value is ExpandoObject expando
                                ? expando.Aggregate(new XElement(fields.Key), itemsToString)
                                : new XElement(fields.Key, fields.Value);
                acc.Add(element);
                return acc;
            };

            XElement multipleItemsToXml(string name, IEnumerable<object> itemCollection)
            {
                var items = itemCollection.Select(i => ((ExpandoObject)i).Aggregate(new XElement(name), itemsToString));
                var root = new XElement($"{name}s", items);
                return root;
            }

            XElement singleItemToXml(string name, ExpandoObject obj)
            {
                return obj.Aggregate(new XElement(name), itemsToString);
            }

            var collectionName = ObjectHelper.GetCollectionFromPath(context.HttpContext.Request.Path.Value);
            var itemName = collectionName.Substring(0, collectionName.Length - 1);

            var xml = context.Object is IEnumerable<object> col ? multipleItemsToXml(itemName, col) : singleItemToXml(itemName, context.Object as ExpandoObject);

            await context.HttpContext.Response.WriteAsync(xml.ToString());
        }
    }
}