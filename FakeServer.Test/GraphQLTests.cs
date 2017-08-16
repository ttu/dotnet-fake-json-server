using JsonFlatFileDataStore;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Xunit;

namespace FakeServer.Test
{
    public class GraphQLTests
    {
        [Fact]
        public async Task Query_Families()
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);

            var q = @"
                    query {
                          families {
                            familyName
                            children(age: 4) {
                              name
                              age
                            }
                          }
                    }";

            var results = await GraphQL.GraphQL.HandleQuery(q, ds);

            var js = JsonConvert.SerializeObject(results);

            Assert.NotEmpty(js);

            UTHelpers.Down(filePath);
        }

        [Theory]
        [InlineData("query { users { name }}")]
        [InlineData("query someName { users { name }}")]
        [InlineData("{ users { name }}")]
        public async Task Query_Users(string q)
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);
            var results = await GraphQL.GraphQL.HandleQuery(q, ds);

            Assert.Equal(4, results.Data["users"].Count);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public async Task Query_Errors()
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);

            var q = @" {  }";

            var results = await GraphQL.GraphQL.HandleQuery(q, ds);

            Assert.Equal(1, results.Errors.Count);

            UTHelpers.Down(filePath);
        }
    }
}