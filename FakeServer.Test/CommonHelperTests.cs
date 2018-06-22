using FakeServer.Common;
using Xunit;

namespace FakeServer.Test
{
    public class CommonHelperTests
    {
        [InlineData("application/json", "application/json")]
        [InlineData("application/json-patch+json", "application/json")]
        [Theory]
        public void ContainsAny_True(string input, params string[] substrings)
        {
            Assert.True(input.ContainsAny(substrings));
        }

        [InlineData("application/json", "application/graphql")]
        [Theory]
        public void ContainsAny_False(string input, params string[] substrings)
        {
            Assert.False(input.ContainsAny(substrings));
        }
    }
}
