using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using System;

namespace FakeServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var dictionary = new Dictionary<string, string>();

            for (int idx = 0; idx < args.Length; idx += 2)
            {
                dictionary.Add(args[idx], args[idx + 1]);
            }

            dictionary.TryGetValue("--filename", out string filename);
            dictionary.TryGetValue("--url", out string url);

            Console.WriteLine($"FileName: {filename ?? "use default"}");
            Console.WriteLine($"Url: {url ?? "use default"}");

            foreach (var kvp in dictionary)
            {
                Startup.MainConfiguration.Add(kvp.Key.Replace("-", ""), kvp.Value);
            }

            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseApplicationInsights();

            if (!string.IsNullOrEmpty(url))
            {
                builder.UseUrls(url);
            }

            var host = builder.Build();
            host.Run();
        }
    }
}