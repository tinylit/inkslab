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
        public Startup()
        {
            using (var startup = new XStartup())
            {
                startup.DoStartup();
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddMvc(options => options.EnableEndpointRouting = false);

            services.DependencyInjection() //? ע�� IConfigureServices ʵ�֡�
                .SeekAssemblies("Inkslab.DI.*")
                .ConfigureByDefined()
                .ConfigureByAuto(new Options.DependencyInjectionOptions()); //? Ĭ��ע�롣

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

            services.AddSwaggerGen(ConfigureSwaggerGen);
        }

        /// <summary>
        ///  Swagger 配置。
        /// </summary>
        /// <param name="options">Swagger 配置。</param>
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

            //? ����
            app.UseCors("Allow")
                .UseRouting()
                .UseMvc()
                .UseEndpoints(x => x.MapControllers());


            app.UseSwagger()
                .UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/" + "swagger:version".Config("1.0.0") + "/swagger.json", "swagger:title".Config("v3")));
        }
    }
}