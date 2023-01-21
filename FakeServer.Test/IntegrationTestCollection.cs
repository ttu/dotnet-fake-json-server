using System.Net.WebSockets;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;

namespace FakeServer.Test
{
    public class IntegrationFixture : ICollectionFixture<WebApplicationFactory<Startup>>, IDisposable
    {
        private WebApplicationFactory<Startup> _factory;
        public HttpClient Client { get; private set; }

        private string _newFilePath;

        public void StartServer(string authenticationType = "")
        {
            var path = Path.GetDirectoryName(typeof(IntegrationFixture).GetTypeInfo().Assembly.Location);
            var fileName = Guid.NewGuid().ToString();
            _newFilePath = UTHelpers.Up(fileName);

            var mainConfiguration = new Dictionary<string, string>
            {
                {"currentPath", path},
                {"file", _newFilePath},
                {"DataStore:IdField", "id"},
                {"Caching:ETag:Enabled", "true"}
            };

            if (!string.IsNullOrEmpty(authenticationType))
            {
                mainConfiguration.Add("Authentication:Enabled", "true");
                mainConfiguration.Add("Authentication:AuthenticationType", authenticationType);

                if (authenticationType == "apikey")
                {
                    mainConfiguration.Add("Authentication:ApiKey", "correct-api-key");
                }
                else
                {
                    mainConfiguration.Add("Authentication:Users:0:Username", "admin");
                    mainConfiguration.Add("Authentication:Users:0:Password", "root");
                }
            }

            _factory = new WebApplicationFactory<Startup>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("IntegrationTest")
                           .ConfigureAppConfiguration((ctx, b) =>
                           {
                               b.SetBasePath(path)
                                .Add(new MemoryConfigurationSource
                                {
                                    InitialData = mainConfiguration
                                });
                           });
                });

            Client = _factory.CreateClient();
        }

        public HttpClient CreateClient(bool allowAutoRedirect)
        {
            return _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = allowAutoRedirect
            });
        }

        public async Task<WebSocket> CreateWebSocketClient()
        {
            return await _factory.Server
                                 .CreateWebSocketClient()
                                 .ConnectAsync(new Uri(_factory.Server.BaseAddress, "ws"), CancellationToken.None);
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            if (Client != null)
            {
                Client.Dispose();
                Client = null;
            }

            if (_factory != null)
            {
                _factory.Dispose();
                _factory = null;
            }

            UTHelpers.Down(_newFilePath);
        }
    }

    [CollectionDefinition("Integration collection")]
    public class IntegrationTestCollection : ICollectionFixture<IntegrationFixture>
    { }
}