using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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
                {"Caching:ETag:Enabled", "true"}
            };

            if (!string.IsNullOrEmpty(authenticationType))
            {
                mainConfiguration.Add("Authentication:Enabled", "true");
                mainConfiguration.Add("Authentication:AuthenticationType", authenticationType);
                mainConfiguration.Add("Authentication:Users:0:Username", "admin");
                mainConfiguration.Add("Authentication:Users:0:Password", "root");
            }

            _factory = new WebApplicationFactory<Startup>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Development")
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
            return _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = allowAutoRedirect });
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            Client.Dispose();
            _factory.Dispose();
            UTHelpers.Down(_newFilePath);
        }

        public int Port { get; private set; }

        public string BaseUrl { get; private set; }
    }

    [CollectionDefinition("Integration collection")]
    public class IntegrationTestCollection : ICollectionFixture<IntegrationFixture>
    {
    }
}