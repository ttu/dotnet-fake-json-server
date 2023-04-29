using System.Diagnostics;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore;
using Serilog;

namespace FakeServer;

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

        IConfigurationRoot config;

        try
        {
            config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddJsonFile("authentication.json", optional: true, reloadOnChange: true)
                .AddInMemoryCollection(initialData)
                .AddEnvironmentVariables()
                .Build();
        }
        catch (Exception ex)
        {
            Console.WriteLine("\nConfiguration file is not valid");
            Console.WriteLine(ex.Message);
            Console.WriteLine("Program will exit...");
            return 1;
        }

        if (!IsConfigValid(config))
        {
            Console.WriteLine("\nUpdate appsettings.json to latest version");
            Console.WriteLine("Program will exit...");
            return 1;
        }

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .WriteTo.RollingFile(Path.Combine(AppContext.BaseDirectory, "log-{Date}.txt"))
            .CreateLogger();

        try
        {
            Log.Information("Starting Fake JSON Server");
            CreateWebHostBuilder(args).UseConfiguration(config).Build().Run();
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

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .UseSerilog();

    private static bool IsConfigValid(IConfigurationRoot config) => config["DataStore:IdField"] != null;

    private static CommandLineApplication BuildCommandLineApp(
        Func<string[], Dictionary<string, string>, int> invoke)
    {
        var app = new CommandLineApplication
        {
            UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.StopParsingAndCollect
        };
        app.HelpOption();

        var optionVersion = app.Option("--version", "Prints the version of the app", CommandOptionType.NoValue);
        var optionFile = app.Option<string>("--file <FILE>", "Data store's JSON file (default datastore.json)",
            CommandOptionType.SingleValue);
        var optionServe = app.Option("-s|--serve <PATH>", "Static files (default wwwroot)",
            CommandOptionType.SingleValue);
        var optionsUrls = app.Option("--urls <URLS>", "Server url (default http://localhost:57602)", CommandOptionType.SingleValue);
        var optionInit = app.Option("--init", "Initializes a new appsettings.json file in the current folder", CommandOptionType.NoValue);

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

                initialData.Add("staticFolder", Path.GetFullPath(optionServe.Value()!));
                Console.WriteLine($"Static files: {initialData["staticFolder"]}");
                // When user defines static files, fake server is used only to server static files
            }
            else
            {
                Console.WriteLine($"Datastore file: {initialData["file"]}");
                Console.WriteLine($"Datastore location: {initialData["currentPath"]}");
                Console.WriteLine($"Static files: default wwwroot");
            }

            if (optionInit.HasValue())
            {
                var baseAppSettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                var newAppSettingFile = Path.Combine(initialData["currentPath"], "appsettings.json");
                if (!File.Exists(newAppSettingFile))
                {
                    File.Copy(baseAppSettingsFile, newAppSettingFile);
                    Console.WriteLine($"AppSettings file created in current folder: {initialData["currentPath"]}");
                }
                else
                {
                    Console.WriteLine("AppSettings.json file already exists in current folder!");
                }

                return 0;
            }

            var arguments = new List<string>(app.RemainingArguments);

            if (optionsUrls.HasValue())
            {
                // Add urls back to arguments that are passed to WebHost builder
                arguments.AddRange(new[] { "--urls", optionsUrls.Value() });
            }

            return invoke(arguments.ToArray(), initialData);
        });

        return app;
    }

    private static string GetAssemblyVersion()
    {
        return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
    }
}