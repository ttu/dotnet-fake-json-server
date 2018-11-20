using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace FakeServer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var inMemoryCollection = ParseInMemoryCollection(args);

            Console.WriteLine($"File: {inMemoryCollection["file"]}");

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            var config = new ConfigurationBuilder()
                       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                       .AddJsonFile($"appsettings.{env}.json", optional: true)
                       .AddJsonFile("authentication.json", optional: true, reloadOnChange: true)
                       .AddInMemoryCollection(inMemoryCollection)
                       .AddEnvironmentVariables()
                       .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .WriteTo.RollingFile(Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "log-{Date}.txt"))
                .CreateLogger();

            try
            {
                Log.Information("Starting Fake JSON Server");
                BuildWebHost(args, config).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder BuildWebHost(string[] args, IConfigurationRoot config) =>
           WebHost.CreateDefaultBuilder(args)
               .UseConfiguration(config)
               .UseStartup<Startup>()
               .UseSerilog();

        private static Dictionary<string, string> ParseInMemoryCollection(string[] args)
        {
            var dictionary = new Dictionary<string, string>();

            for (int idx = 0; idx < args.Length; idx += 2)
            {
                dictionary.Add(args[idx], args[idx + 1]);
            }

            var inMemoryCollection = new Dictionary<string, string>();

            foreach (var kvp in dictionary)
            {
                inMemoryCollection.Add(kvp.Key.Replace("-", ""), kvp.Value);
            }

            inMemoryCollection.TryAdd("currentPath", Directory.GetCurrentDirectory());

            if (!inMemoryCollection.ContainsKey("file"))
            {
                dictionary.TryGetValue("--file", out string file);
                inMemoryCollection.Add("file", file ?? "datastore.json");
            }

            return inMemoryCollection;
        }
    }
}