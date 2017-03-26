using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

namespace FakeServer
{
    public class Startup
    {
        // TODO: Add to Configuration
        private readonly string _jsonFileName = "datastore.json";

        private readonly string _path;

        public Startup(IHostingEnvironment env)
        {
            _path = env.ContentRootPath;

            var builder = new ConfigurationBuilder()
                .SetBasePath(_path)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var path = Path.Combine(_path, _jsonFileName);
            services.AddSingleton(typeof(DataStore), new DataStore(path));

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAnyPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseCors("AllowAnyPolicy");

            app.UseMvc();

            app.Map("/status", rootApp =>
            {
                rootApp.Run(context =>
                {
                    context.Response.StatusCode = 200;
                    return context.Response.WriteAsync("{\"status\": \"Ok\"}");
                });
            });
        }
    }
}