using Hangfire;
using Hangfire.Storage.SQLite;
using HappyBirthday.API.Infrastructure;
using HappyBirthday.Interfaces;
using HappyBirthday.Services;
using HappyBirthday.Services.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HappyBirthday
{
    public class Startup
    {
        private string dashboardUrl = "/hangfire";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            //Can change here for other hangfire storage options
            services.AddHangfire(configuration => configuration
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSQLiteStorage());
            services.AddHangfireServer();


            services.AddSingleton<ApiClient>();
            services.AddTransient<IBirthdayService, BirthdayService>();
            services.AddHostedService<Scheduler>();
             
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseHangfireDashboard(dashboardUrl);
        }
    }
}
