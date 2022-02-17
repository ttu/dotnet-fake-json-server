using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace FakeServer.Common
{
    public static class ObjectHelper
    {
        public static Dictionary<string, Func<dynamic, dynamic, bool>> Funcs = new()
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
        /// <param name="idFieldName"></param>
        /// <returns>Dynamic is return value can be a single item or a list</returns>
        public static dynamic GetNestedProperty(ExpandoObject current, string propertyName, string idFieldName)
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
                    returnValue = valueEnumerable.FirstOrDefault(e => CompareFieldValueWithId(e, idFieldName, parsedInteger));
                else
                    returnValue = CompareFieldValueWithId(((dynamic)currentValue), idFieldName, parsedInteger) ? currentValue as ExpandoObject : null;
            }
            else
            {
                returnValue = currentValue;
            }

            return string.IsNullOrEmpty(tail) ? returnValue : GetNestedProperty(returnValue, tail, idFieldName);
        }

        public static dynamic GetFieldValue(object source, string fieldName)
        {
            if (source is ExpandoObject sourceExpando)
            {
                var sourceExpandoDict = new Dictionary<string, dynamic>(sourceExpando, StringComparer.OrdinalIgnoreCase);
                return sourceExpandoDict.ContainsKey(fieldName) ? sourceExpandoDict[fieldName] : null;
            }

            var srcProp = source.GetType().GetProperties().FirstOrDefault(p => string.Equals(p.Name, fieldName, StringComparison.OrdinalIgnoreCase));
            return srcProp?.GetValue(source, null);
        }

        /// <summary>
        /// Compare the field value from a source object to the provided id.
        /// </summary>
        /// <remarks>
        /// If the field value is a string, it is also compared to the string representation of the provided id.
        /// </remarks>
        /// <param name="source"></param>
        /// <param name="fieldName"></param>
        /// <param name="id"></param>
        /// <returns>The field value from is equal to the provided id</returns>
        public static bool CompareFieldValueWithId(object source, string fieldName, dynamic id)
        {
            dynamic fieldValue = ObjectHelper.GetFieldValue(source, fieldName);

            if (fieldValue.Equals(id))
                return true;

            if (fieldValue.GetType() == typeof(string) && id.GetType() != typeof(string))
                return fieldValue == id.ToString().ToLower();

            return false;
        }

        public static void SetFieldValue(object item, string fieldName, dynamic data)
        {
            if (item is JToken)
            {
                dynamic jTokenItem = item;
                jTokenItem[fieldName] = data;
            }
            else if (item is ExpandoObject)
            {
                dynamic expandoItem = item;
                var expandoDict = expandoItem as IDictionary<string, object>;
                expandoDict[fieldName] = data;
            }
            else
            {
                var idProperty = item.GetType().GetProperties().FirstOrDefault(p => string.Equals(p.Name, fieldName, StringComparison.OrdinalIgnoreCase));

                if (idProperty != null && idProperty.CanWrite)
                    idProperty.SetValue(item, data);
            }
        }

        private static object GetValue(object source, dynamic srcProp)
        {
            return source.GetType() == typeof(ExpandoObject)
                        ? srcProp.Value
                        : srcProp.GetValue(source, null);
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

        private static readonly List<Func<string, dynamic>> _convertFuncs = new()
        {
            x => Convert.ToBoolean(x),
            x => Convert.ToInt32(x),
            x => double.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : throw new Exception(),
            x => DateTime.Parse(x, CultureInfo.InvariantCulture)
        };

        private static Lazy<List<Func<string, dynamic>>> _convertFuncsExceptDateTime = new (() => _convertFuncs.Take(3).ToList());
        
        private static List<Func<string, dynamic>> _convertIdFuncs => _convertFuncsExceptDateTime.Value;

        private static dynamic GetValueAsCorrectType(string value, List<Func<string, dynamic>> convertFuncs)
        {
            foreach (var func in convertFuncs)
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
        /// Convert a value to correct type
        /// </summary>
        /// <param name="value">the input value</param>
        /// <returns>value as a boolean, an integer, a double, a DateTime or as a string</returns>
        public static dynamic GetValueAsCorrectType(string value) => GetValueAsCorrectType(value, _convertFuncs);

        /// <summary>
        /// Convert the value of an identifier from JSON to correct type.
        /// </summary>
        /// <param name="id">the input id</param>
        /// <returns>value as a boolean, an integer, a double or as a string</returns>
        public static dynamic GetIdAsCorrectType(string id) => GetValueAsCorrectType(id, _convertIdFuncs);

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

        public static string RemoveLiterals(string input) => Regex.Replace(input, "[\\\\](?=(\"))", "");

        public static string GetCollectionFromPath(string path)
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