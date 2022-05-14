using Hangfire;
using HappyBirthday.Interfaces;
using HappyBirthday.Models;
using Microsoft.Data.Sqlite;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace HappyBirthday.Services.SQLite
{
    public class BirthdayService : IBirthdayService
    {
        //private readonly string connString = string.Format("Data Source={0}", Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"db\hbd.db"));
        //private readonly string hookBin = "https://hookb.in/7Z7ONqR7JOcZl29Xe1my";

        private readonly ApiClient apiClient;
        private readonly AppConfig config;
        private const int totalSecondsInADay = 86400;

        public Instant Now => SystemClock.Instance.GetCurrentInstant();

        public BirthdayService(ApiClient apiClient, AppConfig config)
        {
            this.apiClient = apiClient;
            this.config = config;
        }

        public async Task CreateUserAsync(User user)
        {
            string connString = string.Format("Data Source={0}", Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), config.DBFile));
            using (var connection = new SqliteConnection(connString))
            {
                connection.Open();
                var sql = @$"
                    INSERT INTO user (first_name, last_name, dob, location)
                    VALUES ($first_name,$last_name, date($dob),$location)
                ";
                var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddWithValue("$first_name", user.FirstName);
                command.Parameters.AddWithValue("$last_name", user.LastName);
                command.Parameters.AddWithValue("$dob", user.DOB);
                command.Parameters.AddWithValue("$location", user.Location);

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteUserAsync(string userId)
        {
            string connString = string.Format("Data Source={0}", Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), config.DBFile));
            using (var connection = new SqliteConnection(connString))
            {
                connection.Open();
                var sql = $"DELETE FROM user WHERE id = $userId";
                var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddWithValue("$userId", userId);
                
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<IEnumerable<User>> GetBirthdayUsersAsync()
        {
            string connString = string.Format("Data Source={0}", Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), config.DBFile));
            var dateNow = $"{Now.ToDateTimeUtc().ToString("yyyy-MM-dd")}";
            using (var connection = new SqliteConnection(connString))
            {
                connection.Open();
                string query = @$"
                    SELECT user.id, user.first_name, user.last_name, user.Location, date(user.dob) AS dob
                    FROM user 
                    LEFT JOIN birthday_scheduler 
	                    ON user.id = birthday_scheduler.user_id
                    WHERE YEAR IS NULL
                    UNION 
                    SELECT user.id, user.first_name, user.last_name, user.Location, date(user.dob) AS dob
                    FROM user 
                    LEFT JOIN birthday_scheduler 
	                    ON user.id = birthday_scheduler.user_id
                    GROUP BY user.id, user.first_name, user.last_name, user.Location, date(user.dob) 
                    HAVING YEAR IS NOT NULL AND MAX(YEAR) < (strftime('%Y', DATE('now')) + 0)
                ";
                var command = connection.CreateCommand();
                command.CommandText = query;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    DataTable result = new DataTable();
                    result.Load(reader);
                    var convertedList = (from rw in result.AsEnumerable()
                                         select new User()
                                         {
                                             Id = Convert.ToInt32(rw["id"]),
                                             FirstName = Convert.ToString(rw["first_name"]),
                                             LastName = Convert.ToString(rw["last_name"]),
                                             DOB = Convert.ToDateTime(rw["dob"]),
                                             Location = Convert.ToString(rw["location"]),
                                         }).ToList();

                    return convertedList;
                }
            }
        }

        public async Task SendHappyBirthday(User user)
        {
            TimeSpan deferredDelivery = GetScheduleForGreetings(user);

            // to make sure it's within +/- a day
            if (deferredDelivery.TotalSeconds > -totalSecondsInADay && deferredDelivery.TotalSeconds < totalSecondsInADay)
            {
                await ScheduleGreetingsAsync(user, deferredDelivery).ConfigureAwait(false);
            }
        }

        public async Task ScheduleGreetingsAsync(User user, TimeSpan deferredDelivery)
        {
            // schedule the second hangfire here, to SayHappyBirthday.
            var greetingId = await this.StoreGreetingsAsync(user);
            BackgroundJob.Schedule(() => SayHappyBirthdayAsync(greetingId, user), deferredDelivery);
            
        }

        public TimeSpan GetScheduleForGreetings(User user)
        {
            var userZone = DateTimeZoneProviders.Tzdb[user.Location];
            var schedule = new LocalDateTime(Now.InUtc().Year, user.DOB.Month, user.DOB.Day, 9, 00);
            var localSchedule = userZone.AtStrictly(schedule);
            var localTime = Now.InZone(userZone);

            return localSchedule.Minus(localTime).ToTimeSpan();
        }

        public async Task<Int64> StoreGreetingsAsync(User user)
        {
            string connString = string.Format("Data Source={0}", Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), config.DBFile));
            using (var connection = new SqliteConnection(connString))
            {
                connection.Open();

                string query = @$"
                    INSERT INTO birthday_scheduler (user_id, year, scheduled_ts)
                    VALUES ('{user.Id}', '{Now.InUtc().Year}', '{Now.InUtc().ToDateTimeUtc()}')
                ";
                var command = connection.CreateCommand();
                command.CommandText = query;
                await command.ExecuteNonQueryAsync();

                command.CommandText = "select last_insert_rowid()";

                // The row ID is a 64-bit value - cast the Command result to an Int64.
                Int64 LastRowID64 = (Int64) await command.ExecuteScalarAsync();

                return LastRowID64;
            }
        }

        public async Task UpdateGreetingsAsync(Int64 GreetingId, User user)
        {
            string connString = string.Format("Data Source={0}", Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), config.DBFile));
            using (var connection = new SqliteConnection(connString))
            {
                connection.Open();
                string query = @$"
                    UPDATE birthday_scheduler SET sent_ts = '{Now.InUtc().ToDateTimeUtc()}'
                    WHERE Id = '{GreetingId}'
                ";

                var command = connection.CreateCommand();
                command.CommandText = query;
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task SayHappyBirthdayAsync(Int64 greetingId, User user)
        {
            var data = $"Hey, {user.FirstName} {user.LastName} it's your birthday.";
            await apiClient.SendRequest<UserResponse>(config.Hookbin, HttpMethod.Post, data);
            await UpdateGreetingsAsync(greetingId, user);
        }
    }
}
