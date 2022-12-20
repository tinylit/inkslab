using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO;

namespace Inkslab.DI.Tests
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddMvc(options => options.EnableEndpointRouting = false);

            services.ConfigureByDefined() //? 注入 IConfigureServices 实现。
                .ConfigureByAuto(new Options.DependencyInjectionOptions()); //? 默认注入。

            services.AddCors(options =>
            {
                options.AddPolicy("Allow",
                    builder =>
                    {
                        builder.SetIsOriginAllowed(origin => true)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });

            //增加XML文档解析
            services.AddSwaggerGen(ConfigureSwaggerGen);
        }

        /// <summary>
        /// 配置SwaggerGen。
        /// </summary>
        /// <param name="options">SwaggerGen配置项。</param>
        protected virtual void ConfigureSwaggerGen(SwaggerGenOptions options)
        {
            options.SwaggerDoc("swagger:version".Config("1.0.0"), new OpenApiInfo { Title = "swagger:title".Config("v3"), Version = "v3" });

            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                options.IncludeXmlComments(file);
            }

            options.IgnoreObsoleteActions();

            options.IgnoreObsoleteProperties();

            options.CustomSchemaIds(x => x.FullName);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            //? 跨域
            app.UseCors("Allow")
                .UseRouting()
                .UseMvc()
                .UseEndpoints(x => x.MapControllers());


            app.UseSwagger()
                .UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/" + "swagger:version".Config("1.0.0") + "/swagger.json", "swagger:title".Config("v3")));
        }
    }
}
