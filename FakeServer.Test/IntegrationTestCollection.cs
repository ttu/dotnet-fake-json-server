using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
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
                TestServer.Run(BaseUrl, dir);
            });

            var success = WaitForServer().Result;
        }

        public void Dispose()
        {
            TestServer.Stop();
        }

        public string BaseUrl { get; private set; }

        private async Task<bool> WaitForServer()
        {
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < 10000)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var result = await client.GetAsync($"{BaseUrl}/status");
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }

            throw new Exception("Server not started");
        }
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