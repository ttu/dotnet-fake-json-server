using FakeServer.Authentication;
using FakeServer.Authentication.Basic;
using FakeServer.Authentication.Custom;
using FakeServer.Authentication.Jwt;
using FakeServer.Common;
using FakeServer.GraphQL;
using FakeServer.Jobs;
using FakeServer.Simulate;
using FakeServer.WebSockets;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using System.Collections.Generic;
using System.IO;

namespace FakeServer
{
    public class Startup
    {
        // TODO: How to pass configuration from Main to Startup?
        public static Dictionary<string, string> MainConfiguration = new Dictionary<string, string>();

        private readonly string _jsonFileName;
        private readonly string _path;

        public Startup(IHostingEnvironment env)
        {
            _path = env.ContentRootPath;
            _jsonFileName = MainConfiguration.ContainsKey("file") ? MainConfiguration["file"] : "datastore.json";

            var builder = new ConfigurationBuilder()
                .SetBasePath(_path)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("authentication.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddInMemoryCollection(MainConfiguration)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            var logConfig = new LoggerConfiguration()
                           .WriteTo.RollingFile(Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "log-{Date}.txt"));

            if (env.IsDevelopment())
                logConfig = logConfig.MinimumLevel.Information();
            else
                logConfig = logConfig.MinimumLevel.Error();

            Log.Logger = logConfig.CreateLogger();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var path = Path.Combine(_path, _jsonFileName);
            services.AddSingleton<IDataStore>(new DataStore(path, reloadBeforeGetCollection: Configuration.GetValue<bool>("Common:EagerDataReload")));
            services.AddSingleton<IMessageBus, MessageBus>();
            services.AddSingleton(typeof(JobsService));

            services.Configure<AuthenticationSettings>(Configuration.GetSection("Authentication"));
            services.Configure<ApiSettings>(Configuration.GetSection("Api"));
            services.Configure<JobsSettings>(Configuration.GetSection("Jobs"));
            services.Configure<SimulateSettings>(Configuration.GetSection("Simulate"));

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAnyPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            if (Configuration.GetValue<bool>("Authentication:Enabled"))
            {
                if (Configuration.GetValue<string>("Authentication:AuthenticationType") == "token")
                {
                    TokenConfiguration.Configure(services);
                }
                else
                {
                    BasicAuthenticationConfiguration.Configure(services);
                }
            }
            else
            {
                AllowAllAuthenticationConfiguration.Configure(services);
            }

            services.AddResponseCaching();

            services.AddMvc(options =>
            {
                options.CacheProfiles.Add("Default",
                    new CacheProfile()
                    {
                        Location = ResponseCacheLocation.Any,
                        VaryByHeader = "User-Agent",
                        Duration = 120
                    });
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Fake JSON API", Version = "v1" });

                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var xmlPath = Path.Combine(basePath, "FakeServer.xml");
                c.IncludeXmlComments(xmlPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            loggerFactory.AddSerilog();
            // Ensure any buffered events are sent at shutdown
            appLifetime.ApplicationStopped.Register(Log.CloseAndFlush);

            // Server is used as an API, so we rarely need exception page
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}

            app.UseCors("AllowAnyPolicy");

            app.UseMiddleware<OptionsMiddleware>();

            if (Configuration.GetValue<bool>("Simulate:Delay:Enabled"))
            {
                app.UseMiddleware<DelayMiddleware>();
            }

            if (Configuration.GetValue<bool>("Simulate:Error:Enabled"))
            {
                app.UseMiddleware<ErrorMiddleware>();
            }

            app.UseWebSockets();

            app.UseMiddleware<NotifyWebSocketMiddlerware>();
            app.UseMiddleware<WebSocketMiddleware>();

            // Authentication must be always used as we have Authorize attributes in use
            // When Authentication is turned off, special AllowAll hander is used
            app.UseAuthentication();

            var useAuthentication = Configuration.GetValue<bool>("Authentication:Enabled");

            if (useAuthentication)
            {
                if (Configuration.GetValue<string>("Authentication:AuthenticationType") == "token")
                {
                    TokenConfiguration.UseTokenProviderMiddleware(app);
                }
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseMiddleware<GraphQLMiddleware>(app.ApplicationServices.GetRequiredService<IDataStore>(), useAuthentication);

            app.UseResponseCaching();

            app.UseMvc();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fake JSON API V1");
            });
        }
    }
}