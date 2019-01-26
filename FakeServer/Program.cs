using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FakeServer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = BuildCommandLineApp(Run);
            return app.Execute(args);
        }

        private static int Run(string[] args, Dictionary<string, string> initialData)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            var config = new ConfigurationBuilder()
                       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                       .AddJsonFile($"appsettings.{env}.json", optional: true)
                       .AddJsonFile("authentication.json", optional: true, reloadOnChange: true)
                       .AddInMemoryCollection(initialData)
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

        private static CommandLineApplication BuildCommandLineApp(Func<string[], Dictionary<string, string>, int> invoke)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);
            app.HelpOption();
            var optionVersion = app.Option("--version", "Prints the version of the app", CommandOptionType.NoValue);
            var optionFile = app.Option("--file", "Data store's JSON file (default datastore.json)", CommandOptionType.SingleOrNoValue);
            var optionServe = app.Option("-s|--serve", "Static files (default wwwroot)", CommandOptionType.SingleOrNoValue);
            app.OnExecute(() =>
            {
                if (optionVersion.HasValue())
                {
                    Console.WriteLine(GetAssemblyVersion());
                    return 0;
                }
                var initialData = new Dictionary<string, string>()
                {
                    { "file", optionFile.HasValue() ? optionFile.Value() : "datastore.json" }
                };
                initialData.TryAdd("currentPath", Directory.GetCurrentDirectory());
                if (optionServe.HasValue() && !string.IsNullOrEmpty(optionServe.Value()))
                {
                    if (!Directory.Exists(optionServe.Value()))
                    {
                        Console.WriteLine($"Folder doesn't exist: {optionServe.Value()}");
                        return 1;
                    }
                    initialData.Add("staticFolder", Path.GetFullPath(optionServe.Value()));
                    Console.WriteLine($"Static files: {initialData["staticFolder"]}");
                    // When user defines static files, fake server is used only to server static files
                }
                else
                {
                    Console.WriteLine($"Datastore file: {initialData["file"]}");
                    Console.WriteLine($"Datastore location: {initialData["currentPath"]}");
                    Console.WriteLine($"Static files: default wwwroot");
                }
                return invoke(app.RemainingArguments.ToArray(), initialData);
            });
            return app;
        }

        private static string GetAssemblyVersion()
        {
            return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
        }
    }
}