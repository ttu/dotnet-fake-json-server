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
using Pluralize.NET.Core;

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

        private static Pluralizer _pluralizer = new Pluralizer();

        public async override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            XElement HandleExpandoField(XElement acc, KeyValuePair<string, object> fields)
            {
                XElement element = null;


                if (fields.Value is IEnumerable<object> innerList)
                {
                    var children = innerList.Select((i) => ((ExpandoObject)i).Aggregate(new XElement($"{_pluralizer.Singularize(fields.Key)}"), HandleExpandoField));
                    element = new XElement(_pluralizer.Pluralize(fields.Key), children);
                }
                else
                {
                    element = fields.Value is ExpandoObject expando
                                 ? expando.Aggregate(new XElement(fields.Key), HandleExpandoField)
                                 : new XElement(fields.Key, fields.Value);
                }

                acc.Add(element);
                return acc;
            };

            XElement MultipleItemsToXml(string name, IEnumerable<object> itemCollection)
            {
                var children = itemCollection.Select((i) => ((ExpandoObject)i).Aggregate(new XElement($"{_pluralizer.Singularize(name)}"), HandleExpandoField));
                var root = new XElement(_pluralizer.Pluralize(name), children);
                return root;
            }

            XElement SingleItemToXml(string name, ExpandoObject obj)
            {
                return obj.Aggregate(new XElement(name), HandleExpandoField);
            }

            var itemName = _pluralizer.Singularize(ObjectHelper.GetCollectionFromPath(context.HttpContext.Request.Path.Value));

            var xml = context.Object is IEnumerable<object> col ? MultipleItemsToXml(itemName, col) : SingleItemToXml(itemName, context.Object as ExpandoObject);

            await context.HttpContext.Response.WriteAsync(xml.ToString());
        }
    }
}