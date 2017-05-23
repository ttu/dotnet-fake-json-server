using FakeServer.Authentication;
using FakeServer.Authentication.Custom;
using FakeServer.Authentication.Jwt;
using FakeServer.Jobs;
using FakeServer.WebSockets;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
            _jsonFileName = MainConfiguration.ContainsKey("filename") ? MainConfiguration["filename"] : "datastore.json";

            var builder = new ConfigurationBuilder()
                .SetBasePath(_path)
                .AddInMemoryCollection(MainConfiguration)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("authentication.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                           .MinimumLevel.Debug()
                           .WriteTo.RollingFile(Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "log-{Date}.txt"))
                           .CreateLogger();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var path = Path.Combine(_path, _jsonFileName);
            services.AddSingleton<IDataStore>(new DataStore(path));
            services.AddSingleton<IMessageBus, MessageBus>();
            services.AddSingleton(typeof(JobsService));

            services.Configure<AuthenticationSettings>(Configuration.GetSection("Authentication"));
            services.Configure<JobsSettings>(Configuration.GetSection("Jobs"));

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAnyPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddMvc();

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

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("AllowAnyPolicy");

            // Status map should be before all middlewares as we don't want to use any of those with status
            app.Map("/status", rootApp =>
            {
                rootApp.Run(context =>
                {
                    context.Response.StatusCode = 200;
                    return context.Response.WriteAsync("{\"status\": \"Ok\"}");
                });
            });

            app.UseWebSockets();

            app.UseMiddleware<NotifyWebSocketMiddlerware>();
            app.UseMiddleware<WebSocketMiddleware>();

            if (Configuration.GetValue<bool>("Authentication:Enabled"))
            {
                TokenConfiguration.Configure(app);
            }
            else
            {
                app.UseMiddleware<AllowAllAuthenticationMiddleware>();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseMvc();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fake JSON API V1");
            });
        }
    }
}