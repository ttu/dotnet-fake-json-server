using FakeServer.Common;
using FakeServer.WebSockets;
using Newtonsoft.Json;
using System;
using System.Dynamic;
using System.Linq;
using System.Threading;
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
        public void GetValueAsCorrectType()
        {
            Assert.IsType<int>(ObjectHelper.GetValueAsCorrectType("2"));
            Assert.IsType<double>(ObjectHelper.GetValueAsCorrectType("2.1"));
            Assert.IsType<DateTime>(ObjectHelper.GetValueAsCorrectType("7/31/2017"));
            Assert.IsType<string>(ObjectHelper.GetValueAsCorrectType("somevalue"));
        }

        [Fact]
        public void OperatorFunctions()
        {
            Assert.True(ObjectHelper.Funcs[""](2, 2));
            Assert.True(ObjectHelper.Funcs[""]("ok", "ok"));
            Assert.False(ObjectHelper.Funcs[""]("ok2", "ok"));
            Assert.True(ObjectHelper.Funcs["_ne"](3.5, 2.3));
            Assert.True(ObjectHelper.Funcs["_ne"]("ok2", "ok"));
            Assert.True(ObjectHelper.Funcs["_lt"](2, 3));
            Assert.True(ObjectHelper.Funcs["_lt"](2.9999, 3));
            Assert.True(ObjectHelper.Funcs["_lte"](2, 2));
            Assert.True(ObjectHelper.Funcs["_lte"](2.0, 2.0));
            Assert.True(ObjectHelper.Funcs["_gt"](3, 2));
            Assert.False(ObjectHelper.Funcs["_gt"](3.0, 3.1));
            Assert.True(ObjectHelper.Funcs["_gte"](3, 2));
        }

        [Fact]
        public void SelectFields()
        {
            dynamic exp = new ExpandoObject();
            exp.Name = "Jim";
            exp.Age = 20;
            exp.Occupation = "Gardener";

            dynamic exp2 = new ExpandoObject();
            exp2.Name = "Danny";
            exp2.Age = 40;
            exp2.Occupation = "Engineer";

            var result = ObjectHelper.SelectFields(new[] { exp, exp2 }, new[] { "Occupation", "Age" });
            Assert.Equal(2, result.Count());
            Assert.Equal(20, result.ToList()[0]["Age"]);
            Assert.Equal(40, result.ToList()[1]["Age"]);
        }

        private class BusMessage
        {
            public string Message { get; set; }
        }

        [Fact]
        public void MessageBus_Subscribe_MessageChanged()
        {
            var bus = new MessageBus();

            var message = new BusMessage { Message = "Hello" };
            var message2 = new BusMessage { Message = "Hello2" };

            var are = new AutoResetEvent(false);

            bus.Subscribe("1", (dynamic m) =>
            {
                Thread.Sleep(1500);
                Assert.Equal("Changed", m.Message);
                // This should be the last one
                are.Set();
            });

            bus.Subscribe("1", (dynamic m) =>
            {
                Thread.Sleep(1);
                Assert.Equal("Hello", m.Message);
                // As this MessageBus is pretty simple, should use immutable objects for sending data
                message.Message = "Changed";
            });

            bus.Subscribe("1", (dynamic m) =>
            {
                Thread.Sleep(500);
                Assert.Equal("Changed", m.Message);
            });

            bus.Subscribe("2", (dynamic m) =>
            {
                Thread.Sleep(5);
                Assert.Equal("Hello2", m.Message);
            });

            bus.Publish("1", message);
            bus.Publish("2", message2);

            var success = are.WaitOne();

            Assert.True(success);
        }

        [Theory]
        [InlineData("[{\"id\":1,\"name\":\"Jam\"es\"}]", "[{\"id\":1,\"name\":\"Jam\"es\"}]")]
        public void RemoveLiterals(string original, string expected)
        {
            var result = ObjectHelper.RemoveLiterals(original);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("/api/users/1/2/3", "users")]
        [InlineData("/api/users?hello=test", "users")]
        public void GetCollectionFromPath(string request, string expected)
        {
            var result = ObjectHelper.GetCollectionFromPath(request);
            Assert.Equal(expected, result);
        }
    }
}