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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.IO;

namespace FakeServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var folder = Configuration["staticFolder"];

            if (!string.IsNullOrEmpty(folder))
            {
                services.AddSpaStaticFiles((spa) =>
                {
                    spa.RootPath = folder;
                });

                // No need to define anything else as this can only be used as a SPA server
                return;
            }

            var jsonFilePath = Path.Combine(Configuration["currentPath"], Configuration["file"]);
            services.AddSingleton<IDataStore>(new DataStore(jsonFilePath, reloadBeforeGetCollection: Configuration.GetValue<bool>("Common:EagerDataReload")));
            services.AddSingleton<IMessageBus, MessageBus>();
            services.AddSingleton<JobsService>();

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

            var useAuthentication = Configuration.GetValue<bool>("Authentication:Enabled");

            if (useAuthentication)
            {
                if (Configuration["Authentication:AuthenticationType"] == "token")
                {
                    services.AddJwtBearerAuthentication();
                }
                else
                {
                    services.AddBasicAuthentication();
                }
            }
            else
            {
                services.AddAllowAllAuthentication();
            }

            services.AddMvc();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Fake JSON API", Version = "v1" });

                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var xmlPath = Path.Combine(basePath, "FakeServer.xml");
                c.IncludeXmlComments(xmlPath);

                if (useAuthentication)
                {
                    c.OperationFilter<AddAuthorizationHeaderParameterOperationFilter>();

                    if (Configuration["Authentication:AuthenticationType"] == "token")
                        c.DocumentFilter<AuthTokenOperation>();
                }
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            app.UseDefaultFiles();

            if (string.IsNullOrEmpty(Configuration["staticFolder"]))
            {
                app.UseStaticFiles();
            }
            else
            {
                app.UseSpa(spa =>
                {
                    spa.ApplicationBuilder.UseSpaStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(Configuration["staticFolder"])
                    });
                });

                // No need to define anything else as this can only be used as a SPA server
                return;
            }

            app.UseCors("AllowAnyPolicy");

            app.UseMiddleware<HttpOptionsMiddleware>();

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

            if (useAuthentication && Configuration["Authentication:AuthenticationType"] == "token")
            {
                app.UseTokenProviderMiddleware();
            }

            if (Configuration.GetValue<bool>("Caching:ETag:Enabled"))
            {
                app.UseMiddleware<ETagMiddleware>();
            }

            app.UseMiddleware<GraphQLMiddleware>(
                        app.ApplicationServices.GetRequiredService<IDataStore>(),
                        app.ApplicationServices.GetRequiredService<IMessageBus>(),
                        useAuthentication);

            app.UseMvc();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fake JSON API V1");
                c.SupportedSubmitMethods(SubmitMethod.Get, SubmitMethod.Head, SubmitMethod.Post, SubmitMethod.Put, SubmitMethod.Patch, SubmitMethod.Delete);
            });
        }
    }
}