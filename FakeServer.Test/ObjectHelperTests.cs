using FakeServer.Common;
using Newtonsoft.Json;
using Xunit;

namespace FakeServer.Test
{
    public class ObjectHelperTests
    {
        [Fact]
        public void WebSocketMessage()
        {
            // Anonymous type is generated as internal so we cant test it withou new serialize/deserialize
            // Should add InternalsVisibleTo or just crate a Typed class
            dynamic original = ObjectHelper.GetWebSocketMessage("POST", "/api/humans/2");

            var msg = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(original));

            Assert.Equal("/api/humans/2", msg.Path.Value);
            Assert.Equal("humans", msg.Collection.Value);
            Assert.Equal("2", msg.ItemId.Value);
            Assert.Equal("POST", msg.Method.Value);

            original = ObjectHelper.GetWebSocketMessage("PUT", "api/humans/2/");

            msg = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(original));

            Assert.Equal("api/humans/2/", msg.Path.Value);
            Assert.Equal("humans", msg.Collection.Value);
            Assert.Equal("2", msg.ItemId.Value);
            Assert.Equal("PUT", msg.Method.Value);

            original = ObjectHelper.GetWebSocketMessage("POST", "/api/humans");

            msg = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(original));

            Assert.Equal("/api/humans", msg.Path.Value);
            Assert.Equal("humans", msg.Collection.Value);
            Assert.Equal(null, msg.ItemId.Value);
            Assert.Equal("POST", msg.Method.Value);
        }

        [Fact]
        public void GetValueAsCorrectType_Integer()
        {
            string a = "2";
            var retVal = ObjectHelper.GetValueAsCorrectType(a);
            Assert.IsType<int>(retVal);
        }

        [Fact]
        public void GetValueAsCorrectType_String()
        {
            string a = "somevalue";
            var retVal = ObjectHelper.GetValueAsCorrectType(a);
            Assert.IsType<string>(retVal);
        }
    }
}