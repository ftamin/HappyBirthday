using HappyBirthday.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HappyBirthday
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog((builderContext, loggerConfig) =>
                {
                    loggerConfig.ReadFrom.Configuration(builderContext.Configuration);
                    loggerConfig.WriteTo.File(@"logs\log-.txt", rollingInterval: RollingInterval.Day, shared: true);
                }).ConfigureServices((hostContext, services) =>
                {
                    var config = hostContext.Configuration;
                    var rootConfig = new AppConfig();
                    config.Bind("AppConfig", rootConfig);
                    services.AddSingleton(rootConfig);
                });
    }
}
