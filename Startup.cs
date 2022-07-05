using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using FluentValidation.AspNetCore;
using StackExchange.Redis;

namespace CyNewsCorner
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
            // services.AddDbContext<CyNewsCornerContext>(ServiceLifetime.Singleton);
            services.AddControllers();
            services.AddHostedService<BackgroundService>();
            services.AddMvc().AddFluentValidation();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    });
            });
            services.AddRouting(r => r.SuppressCheckForUnhandledSecurityMetadata = true);

            //redis
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(string.Format("{0}:{1},{2},{3},{4}", Configuration["redisHost"], Configuration["redisPort"], "ssl=false", "allowAdmin=true", "password=" + Configuration["redisPwd"]));
            //var redis = ConnectionMultiplexer.Connect("ec2-44-205-210-221.compute-1.amazonaws.com:15609,ssl=false,password=p8a7f5399b58dbe470e2932869f48920e38fa6e509d4ebce159d1ab49b3d40423");
            services.AddSingleton<IConnectionMultiplexer>(redis);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder =>
            {
                builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            });
            app.Use((context, next) =>
            {
                context.Items["__CorsMiddlewareInvoked"] = true;
                return next();
            });
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
