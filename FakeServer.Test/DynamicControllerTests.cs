using FakeServer.Controllers;
using JsonFlatFileDataStore;
using System.Linq;
using Xunit;

namespace FakeServer.Test
{
    public class DynamicControllerTests
    {
        [Fact]
        public void GetCollections()
        {
            var filePath = UTHelpers.Up();
            var ds = new DataStore(filePath);

            var controller = new DynamicController(ds);

            var collections = controller.GetCollections();
            Assert.Equal(3, collections.Count());

            UTHelpers.Down(filePath);
        }
    }
}