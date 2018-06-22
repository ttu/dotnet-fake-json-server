namespace FakeServer.Common
{
    public static class CommonHelper
    {
        /// <summary>
        /// Checks if input string contains any of the provided substrings.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="substrings"></param>
        /// <returns></returns>
        public static bool ContainsAny(this string input, params string[] substrings)
        {
            foreach (var substring in substrings)
            {
                if (input.Contains(substring))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
