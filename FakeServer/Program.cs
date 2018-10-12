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
        public static Dictionary<string, string> MainConfiguration = new Dictionary<string, string>();

        public static IConfiguration Configuration { get; set; }

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
                MainConfiguration.Add(kvp.Key.Replace("-", ""), kvp.Value);
            }

            MainConfiguration.Add("currentPath", Directory.GetCurrentDirectory());

            if (!MainConfiguration.ContainsKey("file"))
                MainConfiguration.Add("file", file ?? "datastore.json");

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            Configuration = new ConfigurationBuilder()
                       .SetBasePath(Directory.GetCurrentDirectory())
                       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                       .AddJsonFile($"appsettings.{env}.json", optional: true)
                       .AddJsonFile("authentication.json", optional: true, reloadOnChange: true)
                       .AddInMemoryCollection(MainConfiguration)
                       .AddEnvironmentVariables()
                       .Build();

            var logConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .WriteTo.RollingFile(Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "log-{Date}.txt"));

            if (env == "Production")
                logConfig = logConfig.MinimumLevel.Error();
            else
                logConfig = logConfig.MinimumLevel.Information();

            Log.Logger = logConfig.CreateLogger();

            try
            {
                Log.Information("Starting Fake JSON Server");
                BuildWebHost(args).Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
           WebHost.CreateDefaultBuilder(args)
               .UseConfiguration(Configuration)
               .UseStartup<Startup>()
               .UseSerilog()
               .Build();
    }

    //var config = new ConfigurationBuilder()
    //           .AddCommandLine(args)
    //           .Build();

    //        var builder = new WebHostBuilder()
    //            .UseKestrel()
    //            .UseContentRoot(Directory.GetCurrentDirectory())
    //            .UseConfiguration(config)
    //            .UseIISIntegration()
    //            .UseStartup<Startup>()
    //            .UseApplicationInsights();

    //        var host = builder.Build();
    //        host.Run();
    //    }
}
