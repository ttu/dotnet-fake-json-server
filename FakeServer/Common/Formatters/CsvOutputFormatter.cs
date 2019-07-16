using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeServer.Common.Formatters
{
    public class CsvOutputFormatter : TextOutputFormatter
    {
        public CsvOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/csv"));
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanWriteType(Type type)
        {
            if (typeof(ExpandoObject).IsAssignableFrom(type) || typeof(IEnumerable<object>).IsAssignableFrom(type))
            {
                return base.CanWriteType(type);
            }

            return false;
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var response = context.HttpContext.Response;
            var buffer = new StringBuilder();

            HandleObject(buffer, context.Object, true);

            using (var writer = context.WriterFactory(response.Body, selectedEncoding))
            {
                return writer.WriteAsync(buffer.ToString());
            }
        }

        private static void HandleObject(StringBuilder buffer, object expando, bool isRoot = false)
        {
            if (expando is IEnumerable<object> collection)
            {
                foreach (var item in collection)
                {
                    FormatCsv(buffer, (ExpandoObject)item);

                    if (isRoot)
                    {
                        if (buffer[buffer.Length - 1] == ',')
                        {
                            buffer.Remove(buffer.Length - 1, 1);
                        }

                        buffer.Append(Environment.NewLine);
                    }
                }
            }
            else
            {
                FormatCsv(buffer, (ExpandoObject)expando);

                if (isRoot)
                {
                    if (buffer[buffer.Length - 1] == ',')
                    {
                        buffer.Remove(buffer.Length - 1, 1);
                    }
                }
            }
        }

        private static void FormatCsv(StringBuilder buffer, ExpandoObject item)
        {
            foreach (var field in item)
            {
                if (field.Value is ExpandoObject || field.Value is IEnumerable<object>)
                {
                    HandleObject(buffer, field.Value);
                }
                else
                {
                    buffer.Append(field.Value.ToString());
                    buffer.Append(",");
                }
            }
        }
    }
}