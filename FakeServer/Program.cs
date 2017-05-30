using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using System;
using Microsoft.Extensions.Configuration;

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

            Console.WriteLine($"FileName: {filename ?? "use default"}");

            foreach (var kvp in dictionary)
            {
                Startup.MainConfiguration.Add(kvp.Key.Replace("-", ""), kvp.Value);
            }

            var config = new ConfigurationBuilder()
               .AddCommandLine(args)
               .Build();

            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(config)
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseApplicationInsights();

            var host = builder.Build();
            host.Run();
        }
    }
}