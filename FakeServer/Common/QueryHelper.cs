using FakeServer.Controllers;
using System.Collections.Generic;

namespace FakeServer.Common
{
    public class PaginationHeader
    {
        public string Prev { get; set; }

        public string Next { get; set; }

        public string First { get; set; }

        public string Last { get; set; }
    }

    public static class QueryHelper
    {
        public static PaginationHeader GetPaginationHeader(string url, int totalCount, int skip, int take, string skipWord, string takeWord)
        {
            return new PaginationHeader
            {
                Prev = skip > 0 ? $"{url}?{skipWord}={(skip - take > 0 ? skip - take : 0)}&{takeWord}={(take - skip < 0 ? take : skip)}" : string.Empty,
                Next = totalCount > (skip + take) ? $"{url}?{skipWord}={(skip + take)}&{takeWord}={take}" : string.Empty,
                First = skip > 0 ? $"{url}?{skipWord}=0&{takeWord}={take}" : string.Empty,
                Last = (totalCount - take) > 0 ? $"{url}?{skipWord}={(totalCount - take)}&{takeWord}={take}" : string.Empty
            };
        }

        public static string GetHeaderLink(PaginationHeader header)
        {
            var rows = new List<string>();

            if (!string.IsNullOrEmpty(header.Prev))
                rows.Add($@"<{header.Prev}>; rel=""prev""");
            if (!string.IsNullOrEmpty(header.Next))
                rows.Add($@"<{header.Next}>; rel=""next""");
            if (!string.IsNullOrEmpty(header.First))
                rows.Add($@"<{header.First}>; rel=""first""");
            if (!string.IsNullOrEmpty(header.Last))
                rows.Add($@"<{header.Last}>; rel=""last""");

            return string.Join(",", rows);
        }

        public static dynamic GetResultObject(IEnumerable<dynamic> results, int totalCount, PaginationHeader pg, QueryOptions options)
        {
            var result = new Dictionary<string, object>
            {
                ["results"] = results,
                ["link"] = pg,
                ["count"] = totalCount,
                [options.SkipWord] = options.Skip,
                [options.TakeWord] = options.Take
            };

            return result;
        }
    }
}