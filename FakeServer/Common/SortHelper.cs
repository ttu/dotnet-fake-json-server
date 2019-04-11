using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace FakeServer.Common
{
    public static class SortHelper
    {
        public static IEnumerable<dynamic> SortFields(IEnumerable<dynamic> results, IEnumerable<string> sortFields)
        {
            if (sortFields.Where(e => !string.IsNullOrEmpty(e)).Any() == false)
                return results;

            var isSortDescending = IsSortDescending(sortFields.First());
            var fieldName = RemoveSortDirection(sortFields.First());

            var sortedResults = GetFirstSortResults(results, fieldName, isSortDescending);

            sortFields.Skip(1).ToList().ForEach(x =>
            {
                isSortDescending = IsSortDescending(x);
                fieldName = RemoveSortDirection(x);

                sortedResults = GetRemainingSortResults(sortedResults, fieldName, isSortDescending);
            });

            return sortedResults.AsEnumerable();
        }

        private static IOrderedEnumerable<dynamic> GetFirstSortResults(IEnumerable<dynamic> results, string sortField, bool isSortDescending)
        {
            if (isSortDescending)
                return results.OrderByDescending(x => ParseField(x as ExpandoObject, sortField));
            else
                return results.OrderBy(x => ParseField(x as ExpandoObject, sortField));
        }

        private static IOrderedEnumerable<dynamic> GetRemainingSortResults(IOrderedEnumerable<dynamic> results, string sortField, bool isSortDescending)
        {
            if (isSortDescending)
                return results.ThenByDescending(x => ParseField(x as ExpandoObject, sortField));
            else
                return results.ThenBy(x => ParseField(x as ExpandoObject, sortField));
        }

        private static dynamic ParseField(ExpandoObject s, string field) => (s as IDictionary<string, object>)[field];

        private static string RemoveSortDirection(string sortField) => sortField.Replace("+", "").Replace("-", "").Trim();

        private static bool IsSortDescending(string sortField) => !(char.IsWhiteSpace(sortField[0]) || sortField.Contains("+"));
    }
}