using Hangfire;
using HappyBirthday.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HappyBirthday.API.Infrastructure
{
    public class Scheduler : BackgroundService
    {
        public static readonly string mainJobName = "happybirthdayjob";
        private readonly IBirthdayService service;
        private readonly ILogger<Scheduler> logger;
        public Scheduler(IBirthdayService service, ILogger<Scheduler> logger)
        {
            this.service = service;
            this.logger = logger;
        }

        public async Task ScheduleBirthdays()
        {
            var users = await service.GetBirthdayUsersAsync();

            foreach (var user in users)
            {
                try
                {
                    await service.SendHappyBirthday(user);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString(), ex);
                }
            }
            
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            RecurringJob.AddOrUpdate("HappyBirthdaySenderJob", () => ScheduleBirthdays(), "0 1 * * *", TimeZoneInfo.FindSystemTimeZoneById("Dateline Standard Time"));

            return Task.CompletedTask;
        }
    }
}
