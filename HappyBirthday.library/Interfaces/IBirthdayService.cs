using HappyBirthday.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HappyBirthday.Interfaces
{
    public interface IBirthdayService
    {
        Task<IEnumerable<User>> GetBirthdayUsersAsync();

        Task<Int64> StoreGreetingsAsync(User user);

        Task UpdateGreetingsAsync(Int64 GreetingId, User BirthdayUser);

        Task CreateUserAsync(User newUser);

        Task DeleteUserAsync(string userId);

        Task SendHappyBirthday(User user);

        Task SayHappyBirthdayAsync(Int64 GreetingId, User user);

        Task ScheduleGreetingsAsync(User user, TimeSpan deferredDelivery);

        TimeSpan GetScheduleForGreetings(User user);
    }
}
