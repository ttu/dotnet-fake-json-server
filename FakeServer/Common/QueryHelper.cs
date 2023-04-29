namespace FakeServer.Common;

public class PaginationHeader
{
    public string Prev { get; set; }

    public string Next { get; set; }

    public string First { get; set; }

    public string Last { get; set; }
}

public class QueryOptions
{
    public int Skip { get; set; }

    public int Take { get; set; }

    public string SkipWord { get; set; }

    public string TakeWord { get; set; }

    public bool IsTextSearch { get; set; }

    public List<string> Fields { get; set; }

    public List<string> SortFields { get; set; }

    public List<string> QueryParams { get; set; }

    public bool Validate()
    {
        if (IsTextSearch && QueryParams.Any())
            return false;

        return true;
    }
}

public static class QueryHelper
{
    public static PaginationHeader GetPaginationHeader(string url, int totalCount, int skip, int take, string skipWord, string takeWord)
    {
        if (skipWord == "page" && takeWord == "per_page")
        {
            var totalPages = totalCount / take + (totalCount % take != 0 ? 1 : 0);
            var page = (skip / take) + 1;
            return new PaginationHeader
            {
                Prev = page > 1 ? $"{url}?{skipWord}={(page - 1)}&{takeWord}={take}" : string.Empty,
                Next = page < totalPages ? $"{url}?{skipWord}={(page + 1)}&{takeWord}={take}" : string.Empty,
                First = page != 1 ? $"{url}?{skipWord}=1&{takeWord}={take}" : string.Empty,
                Last = page != totalPages ? $"{url}?{skipWord}={totalPages}&{takeWord}={take}" : string.Empty
            };
        }

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
            [options.SkipWord] = options.SkipWord == "page" ? (options.Skip / options.Take) + 1 : options.Skip,
            [options.TakeWord] = options.Take
        };

        return result;
    }

    public static QueryOptions GetQueryOptions(IQueryCollection query, int skip, int take)
    {
        var skipWord = "skip";
        var takeWord = "take";
        var isTextSearch = false;
        var fields = new List<string>();
        var sortFields = new List<string>();

        var queryParams = query.Keys.ToList();

        if (queryParams.Contains("offset"))
        {
            skip = Convert.ToInt32(query["offset"]);
            queryParams.Remove("offset");
            skipWord = "offset";
        }

        if (queryParams.Contains("limit"))
        {
            take = Convert.ToInt32(query["limit"]);
            queryParams.Remove("limit");
            takeWord = "limit";
        }

        if (queryParams.Contains("per_page"))
        {
            take = Convert.ToInt32(query["per_page"]);
            queryParams.Remove("per_page");
            takeWord = "per_page";
        }

        if (queryParams.Contains("page"))
        {
            var page = Convert.ToInt32(query["page"]);
            skip = (page - 1) * take;
            queryParams.Remove("page");
            skipWord = "page";
        }

        if (queryParams.Contains("q"))
        {
            isTextSearch = true;
            queryParams.Remove("q");
        }

        if (queryParams.Contains("fields"))
        {
            fields = query["fields"].ToString().Split(',').ToList();
            queryParams.Remove("fields");
        }

        if (queryParams.Contains("sort"))
        {
            sortFields = query["sort"].ToString().Split(',').ToList();
            queryParams.Remove("sort");
        }

        queryParams.Remove("skip");
        queryParams.Remove("take");

        return new QueryOptions
        {
            Skip = skip,
            Take = take,
            SkipWord = skipWord,
            TakeWord = takeWord,
            IsTextSearch = isTextSearch,
            Fields = fields,
            SortFields = sortFields,
            QueryParams = queryParams
        };
    }

    public static bool IsQueryValid(IQueryCollection query)
    {
        List<List<string>> validQuerySets = new List<List<string>>
        {
            new List<string> { "skip", "take" },
            new List<string> { "offset", "limit" },
            new List<string> { "page", "per_page" }
        };

        var usedQuerySet = validQuerySets.FirstOrDefault(q => q.Any(query.ContainsKey));
        if (usedQuerySet == null)
            return true;

        var notValidKeys = validQuerySets.Where(q => q != usedQuerySet).SelectMany(q => q);
        return notValidKeys.Any(query.ContainsKey) == false;
    }
}