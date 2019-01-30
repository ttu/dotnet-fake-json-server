using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;

namespace FakeServer.Common
{
    public static class ObjectHelper
    {
        public static Dictionary<string, Func<dynamic, dynamic, bool>> Funcs = new Dictionary<string, Func<dynamic, dynamic, bool>>
        {
            [""] = (a, b) => a == b,
            ["_ne"] = (a, b) => a != b,
            ["_lt"] = (a, b) => (a < b),
            ["_gt"] = (a, b) => a > b,
            ["_lte"] = (a, b) => a <= b,
            ["_gte"] = (a, b) => a >= b
        };

        /// <summary>
        /// Find property from ExpandoObject and compare it to provided value
        /// </summary>
        /// <param name="current"></param>
        /// <param name="propertyName"></param>
        /// <param name="valueToCompare"></param>
        /// <param name="compareFunc"></param>
        /// <returns>True is object matches valueToCompare</returns>
        public static bool GetPropertyAndCompare(ExpandoObject current, string propertyName, string valueToCompare, Func<dynamic, dynamic, bool> compareFunc)
        {
            var currentProperty = propertyName.Contains('.') ? propertyName.Split('.').First() : propertyName;
            var tail = propertyName.Contains('.') ? propertyName.Substring(propertyName.IndexOf('.') + 1) : string.Empty;

            var currentAsDict = ((IDictionary<string, object>)current);

            if (!currentAsDict.ContainsKey(currentProperty))
                return false;

            var currentValue = currentAsDict[currentProperty];

            if (string.IsNullOrEmpty(tail))
                return compareFunc(TryToCastValue(currentValue), GetValueAsCorrectType(valueToCompare));

            if (currentValue is IEnumerable<dynamic> valueEnumerable)
                return valueEnumerable.Any(e => GetPropertyAndCompare(e, tail, valueToCompare, compareFunc));
            else
                return GetPropertyAndCompare(currentValue as ExpandoObject, tail, valueToCompare, compareFunc);
        }

        /// <summary>
        /// Find property from ExpandoObject
        ///
        /// Split nested properties with backslash, e.g. child/grandchild/grandgrandchild
        ///
        /// If path contains integers, those are used as id field comparisons
        /// </summary>
        /// <param name="current"></param>
        /// <param name="propertyName"></param>
        /// <returns>Dynamic is return value can be a single item or a list</returns>
        public static dynamic GetNestedProperty(ExpandoObject current, string propertyName)
        {
            var propertyNameCurrent = propertyName.Contains('/') ? propertyName.Split('/').First() : propertyName;
            var tail = propertyName.Contains('/') ? propertyName.Substring(propertyName.IndexOf('/') + 1) : string.Empty;
            var peekProperty = tail.Contains('/') ? tail.Split('/').FirstOrDefault() : tail;

            var currentValue = ((IDictionary<string, object>)current)[propertyNameCurrent];

            dynamic returnValue;

            if (int.TryParse(peekProperty, out int parsedInteger))
            {
                tail = tail.Contains('/') ? tail.Substring(tail.IndexOf('/') + 1) : string.Empty;

                if (currentValue is IEnumerable<dynamic> valueEnumerable)
                    returnValue = valueEnumerable.FirstOrDefault(e => e.id == parsedInteger);
                else
                    returnValue = ((dynamic)currentValue).id == parsedInteger ? currentValue as ExpandoObject : null;
            }
            else
            {
                returnValue = currentValue;
            }

            if (string.IsNullOrEmpty(tail))
                return returnValue;
            else
                return GetNestedProperty(returnValue, tail);
        }

        public static dynamic GetWebSocketMessage(string method, string path)
        {
            var cleaned = path.StartsWith("/") ? path.Substring(1) : path;
            cleaned = cleaned.EndsWith("/") ? cleaned.Substring(0, cleaned.Length - 1) : cleaned;
            cleaned = cleaned.Replace($"{Config.ApiRoute}/", "");

            return new
            {
                Method = method,
                Path = path,
                Collection = cleaned.IndexOf("/") != -1 ? cleaned.Substring(0, cleaned.IndexOf("/")) : cleaned,
                ItemId = cleaned.LastIndexOf("/") != -1 ? cleaned.Substring(cleaned.LastIndexOf("/") + 1) : null
            };
        }

        public static IEnumerable<dynamic> SelectFields(IEnumerable<dynamic> results, IEnumerable<string> fields)
        {
            return results.Select(s => ParseFields(s as ExpandoObject, fields));
        }

        private static dynamic ParseFields(ExpandoObject s, IEnumerable<string> fields)
        {
            var dict = s as IDictionary<string, object>;
            return dict.Where(kvp => fields.Contains(kvp.Key)).ToDictionary(k => k.Key, k => k.Value);
        }

        private static List<Func<string, dynamic>> _convertFuncs = new List<Func<string, dynamic>>
        {
            x => Convert.ToBoolean(x),
            x => Convert.ToInt32(x),
            x => double.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : throw new Exception(),
            x => DateTime.Parse(x, CultureInfo.InvariantCulture)
        };

        /// <summary>
        /// Convert input value to correct type
        /// </summary>
        /// <param name="value">input</param>
        /// <returns>value as an integer, as a double or as a string</returns>
        public static dynamic GetValueAsCorrectType(string value)
        {
            foreach (var func in _convertFuncs)
            {
                try
                {
                    return func(value);
                }
                catch (Exception)
                {
                }
            }

            return value;
        }

        /// <summary>
        /// Try to cast value from JSON file to correct type.
        /// Now only type to handle is DateTime
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static dynamic TryToCastValue(dynamic value)
        {
            if (value is string)
                if (DateTime.TryParse(value, out DateTime result))
                    return result;

            return value;
        }
    }
}