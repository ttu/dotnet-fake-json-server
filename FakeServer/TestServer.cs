using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;

namespace FakeServer
{
    // For running e.g. integration tests
    public static class TestServer
    {
        private static IWebHost _host;

        public static void Run(string url, string path, string file, string authenticationType = "")
        {
           var mainConfiguration = new Dictionary<string, string>();

            mainConfiguration.Add("currentPath", path);
            mainConfiguration.Add("file", file);

            mainConfiguration.Add("Caching:ETag:Enabled", "true");

            if (!string.IsNullOrEmpty(authenticationType))
            {
                mainConfiguration.Add("Authentication:Enabled", "true");
                mainConfiguration.Add("Authentication:AuthenticationType", authenticationType);
                mainConfiguration.Add("Authentication:Users:0:Username", "admin");
                mainConfiguration.Add("Authentication:Users:0:Password", "root");
            }

            var configuration = new ConfigurationBuilder()
                       .SetBasePath(path)
                       .AddInMemoryCollection(mainConfiguration)
                       .Build();

            _host = new WebHostBuilder()
               .UseUrls(url)
               .UseKestrel()
               .UseContentRoot(path)
               .UseConfiguration(configuration)
               .UseStartup<Startup>()
               .Build();

            _host.Run();
        }

        public static void Stop()
        {
            _host?.Dispose();
        }
    }
}