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
        private Task _serverTask;
        private string _newFilePath;

        public IntegrationFixture()
        {
        }

        public void StartServer(string authenticationType = "")
        {
            if (_serverTask != null)
                return;

            var dir = Path.GetDirectoryName(typeof(IntegrationFixture).GetTypeInfo().Assembly.Location);

            var fileName = Guid.NewGuid().ToString();
            _newFilePath = UTHelpers.Up(fileName);

            Port = 5001;
            BaseUrl = $"http://localhost:{Port}";

            _serverTask = Task.Run(() =>
            {
                TestServer.Run(BaseUrl, dir, $"{fileName}.json", authenticationType);
            });

            var success = Task.Run(() => WaitForServer()).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            TestServer.Stop();
            UTHelpers.Down(_newFilePath);
        }

        public int Port { get; private set; }

        public string BaseUrl { get; private set; }

        private async Task<bool> WaitForServer()
        {
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < 20000)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var result = await client.GetAsync($"{BaseUrl}");
                        return true;
                    }
                }
                catch (Exception)
                {
                }

                await Task.Delay(500);
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