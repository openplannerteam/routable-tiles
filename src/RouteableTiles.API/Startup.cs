using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RouteableTiles.API.Results;
using Serilog;

namespace RouteableTiles.API
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
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder => builder.AllowAnyOrigin());
            });
            
            services.AddMvc(options =>
                {
                    options.OutputFormatters.Insert(0, new JsonLDOutputFormatter());
                }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddSerilog();
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseCors("AllowAllOrigins");
            
            var options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
            };
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            
            app.UseForwardedHeaders(options);
            app.Use((context, next) => 
            {
                if (context.Request.Headers.TryGetValue("X-Forwarded-PathBase", out var pathBases))
                {
                    context.Request.PathBase = pathBases.First();
                    if (context.Request.PathBase.Value.EndsWith("/"))
                    {
                        context.Request.PathBase =
                            context.Request.PathBase.Value.Substring(0, context.Request.PathBase.Value.Length - 1);
                    }
                    if (context.Request.Path.Value.StartsWith(context.Request.PathBase.Value))
                    {
                        var before = context.Request.Path.Value;
                        var after = context.Request.Path.Value.Substring(
                            context.Request.PathBase.Value.Length,
                            context.Request.Path.Value.Length - context.Request.PathBase.Value.Length);
                        context.Request.Path = after;
                    }
                }
                return next();
            });
            
            DatabaseInstance.Default = new Database(this.Configuration["db"], 14);

            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}