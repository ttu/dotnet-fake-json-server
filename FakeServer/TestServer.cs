using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace FakeServer
{
    // For running e.g. integration tests
    public static class TestServer
    {
        private static IWebHost _host;

        public static void Run(string url, string path, string file)
        {
            Startup.MainConfiguration.Add("file", file);

            _host = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(path)
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