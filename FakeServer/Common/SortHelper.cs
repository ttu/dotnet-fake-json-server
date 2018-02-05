using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace FakeServer.Common
{
    public static class SortHelper
    {
        public static IEnumerable<dynamic> SortFields(IEnumerable<dynamic> results, IEnumerable<string> sortFields)
        {
            IOrderedEnumerable<dynamic> sortedResults = null;
            int i = 0;

            sortFields.ToList().ForEach(x =>
            {
                bool isSortDescending = IsSortDescending(x);
                string fieldName = RemoveSortDirection(x);

                if (i == 0)
                    sortedResults = GetFirstSortResults(results, fieldName, isSortDescending);
                else
                    sortedResults = GetRemainingSortResults(sortedResults, fieldName, isSortDescending);

                i++;
            });

            return sortedResults.AsEnumerable();
        }

        private static IOrderedEnumerable<dynamic> GetFirstSortResults(IEnumerable<dynamic> results, string sortField, bool isSortDescending)
        {
            IOrderedEnumerable<dynamic> sortResults = null;

            if (isSortDescending)
                sortResults = results.OrderByDescending(x => ParseField(x as ExpandoObject, sortField));
            else
                sortResults = results.OrderBy(x => ParseField(x as ExpandoObject, sortField));

            return sortResults;
        }

        private static IOrderedEnumerable<dynamic> GetRemainingSortResults(IOrderedEnumerable<dynamic> results, string sortField, bool isSortDescending)
        {
            IOrderedEnumerable<dynamic> sortResults = null;

            if (isSortDescending)
                sortResults = results.ThenByDescending(x => ParseField(x as ExpandoObject, sortField));
            else
                sortResults = results.ThenBy(x => ParseField(x as ExpandoObject, sortField));

            return sortResults;
        }

        private static dynamic ParseField(ExpandoObject s, string field)
        {
            return (s as IDictionary<string, object>)[field];
        }

        private static bool IsSortDescending(string sortField)
        {
            if (string.IsNullOrWhiteSpace(sortField[0].ToString()) ||
                sortField.Contains("+")) return false;
            else return true;
        }

        private static string RemoveSortDirection(string sortField)
        {
            string sField = sortField.Replace("+", "").Replace("-", "");
            return sField.Trim();
        }
    }
}
