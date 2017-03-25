using Microsoft.AspNetCore.Hosting;

namespace FakeServer
{
    // For running e.g. integration tests
    public static class TestRunner
    {
        private static IWebHost _host;

        public static void Run(string url, string path)
        {
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
            _host.Dispose();
        }
    }
}