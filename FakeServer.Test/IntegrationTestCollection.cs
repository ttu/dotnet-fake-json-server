using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace FakeServer.Test
{
    public class IntegrationFixture : IDisposable
    {
        private readonly Task _serverTask;

        public IntegrationFixture()
        {
            var dir = Path.GetDirectoryName(typeof(IntegrationFixture).GetTypeInfo().Assembly.Location);
            int port = 5001;
            BaseUrl = $"http://localhost:{port}";

            _serverTask = Task.Run(() =>
            {
                TestRunner.Run(BaseUrl, dir);
            });

            // Give some time to server to start
            Task.Delay(2000);
        }

        public void Dispose()
        {
            TestRunner.Stop();
        }

        public string BaseUrl { get; private set; }
    }

    [CollectionDefinition("Integration collection")]
    public class IntegrationTestCollection : ICollectionFixture<IntegrationFixture>
    {
        private IntegrationFixture _fixture;

        public IntegrationTestCollection(IntegrationFixture fixture)
        {
            _fixture = fixture;
        }
    }
}