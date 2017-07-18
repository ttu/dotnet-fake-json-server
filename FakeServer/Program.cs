using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

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

            dictionary.TryGetValue("--file", out string file);

            Console.WriteLine($"File: {file ?? "use default"}");

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