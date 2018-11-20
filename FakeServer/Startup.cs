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
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.Swagger;
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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var jsonFilePath = Path.Combine(Configuration.GetValue<string>("currentPath"), Configuration.GetValue<string>("file"));
            services.AddSingleton<IDataStore>(new DataStore(jsonFilePath, reloadBeforeGetCollection: Configuration.GetValue<bool>("Common:EagerDataReload")));
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

            var useAuthentication = Configuration.GetValue<bool>("Authentication:Enabled");

            if (useAuthentication)
            {
                if (Configuration.GetValue<string>("Authentication:AuthenticationType") == "token")
                {
                    var blacklistService = new TokenBlacklistService();
                    services.AddSingleton(blacklistService);

                    TokenConfiguration.Configure(services, blacklistService);
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

                    if (Configuration.GetValue<string>("Authentication:AuthenticationType") == "token")
                        c.DocumentFilter<AuthTokenOperation>();
                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
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

            if (useAuthentication && Configuration.GetValue<string>("Authentication:AuthenticationType") == "token")
            {
                TokenConfiguration.UseTokenProviderMiddleware(app);
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

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