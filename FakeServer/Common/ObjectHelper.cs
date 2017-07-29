using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace FakeServer.Common
{
    public static class ObjectHelper
    {
        /// <summary>
        /// Find property from ExpandoObject and compare it to provided value
        /// </summary>
        /// <param name="current"></param>
        /// <param name="propertyName"></param>
        /// <param name="valueToCompare"></param>
        /// <returns>True is object matches valueToCompare</returns>
        public static bool GetPropertyAndCompare(ExpandoObject current, string propertyName, string valueToCompare)
        {
            var currentProperty = propertyName.Contains('.') ? propertyName.Split('.').First() : propertyName;
            var tail = propertyName.Contains('.') ? propertyName.Substring(propertyName.IndexOf('.') + 1) : string.Empty;

            var currentAsDict = ((IDictionary<string, object>)current);

            if (!currentAsDict.ContainsKey(currentProperty))
                return false;

            var currentValue = currentAsDict[currentProperty];

            if (string.IsNullOrEmpty(tail))
                return ((dynamic)currentValue).ToString() == valueToCompare;

            if (currentValue is IEnumerable<dynamic> valueEnumerable)
                return valueEnumerable.Any(e => GetPropertyAndCompare(e, tail, valueToCompare));
            else
                return GetPropertyAndCompare(currentValue as ExpandoObject, tail, valueToCompare);
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
            cleaned = cleaned.Replace("api/", "");

            return new
            {
                Method = method,
                Path = path,
                Collection = cleaned.IndexOf("/") != -1 ? cleaned.Substring(0, cleaned.IndexOf("/")) : cleaned,
                ItemId = cleaned.LastIndexOf("/") != -1 ? cleaned.Substring(cleaned.LastIndexOf("/") + 1) : null
            };
        }

        /// <summary>
        /// Convert input value to correct type
        /// </summary>
        /// <param name="value">input</param>
        /// <returns>value as an integer or as a string</returns>
        public static dynamic GetValueAsCorrectType(string value)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return value;
            }
        }
    }
}