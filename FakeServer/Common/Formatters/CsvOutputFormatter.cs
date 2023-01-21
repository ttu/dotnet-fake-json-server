using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System.Dynamic;
using System.Text;

namespace FakeServer.Common.Formatters;

public class CsvOutputFormatter : TextOutputFormatter
{
    public CsvOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/csv"));
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    protected override bool CanWriteType(Type type) =>
            typeof(ExpandoObject).IsAssignableFrom(type) || typeof(IEnumerable<object>).IsAssignableFrom(type) ? base.CanWriteType(type) : false;

    public async override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        string itemsToString(string acc, dynamic multiItems) => multiItems is IEnumerable<object> collection
                                                                ? collection.Aggregate(acc, itemsToString)
                                                                : string.IsNullOrEmpty(acc) ? multiItems : $"{acc},{multiItems}";

        string multipleItemsToCsv(IEnumerable<object> itemCollection)
        {
            var items = HandleExpandoCollection(itemCollection);
            var allText = items.Select(i => i.Aggregate(string.Empty, itemsToString));
            return string.Join(Environment.NewLine, allText);
        }

        string singleItemToCsv(object obj)
        {
            var items = HandleExpando(context.Object as ExpandoObject);
            return items.Aggregate(string.Empty, itemsToString);
        }

        var text = context.Object is IEnumerable<object> col ? multipleItemsToCsv(col) : singleItemToCsv(context.Object);

        await context.HttpContext.Response.WriteAsync(text);
    }

    private IEnumerable<IEnumerable<dynamic>> HandleExpandoCollection(IEnumerable<object> collection) =>
                                    collection.Select(item => item is IEnumerable<object> col
                                                                ? HandleExpandoCollection(col)
                                                                : HandleExpando(item as ExpandoObject));

    private IEnumerable<dynamic> HandleExpando(ExpandoObject item) =>
        item.Select(field =>
        {
            switch (field.Value)
            {
                case ExpandoObject exp: return HandleExpando(exp);
                case IEnumerable<object> col: return HandleExpandoCollection(col);
                default: return field.Value.ToString() as dynamic;
            }
        });
}