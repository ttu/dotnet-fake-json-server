using Microsoft.AspNetCore.Hosting;

namespace FakeServer
{
    // For running e.g. integration tests
    public static class TestServer
    {
        private static IWebHost _host;

        public static void Run(string url, string path, string file, string authenticationType = "")
        {
            Startup.MainConfiguration.Add("file", file);

            if (!string.IsNullOrEmpty(authenticationType))
            {
                Startup.MainConfiguration.Add("Authentication:Enabled", "true");
                Startup.MainConfiguration.Add("Authentication:AuthenticationType", authenticationType);
            }

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