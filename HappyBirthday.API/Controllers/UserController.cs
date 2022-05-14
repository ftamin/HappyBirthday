using HappyBirthday.Interfaces;
using HappyBirthday.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace HappyBirthday.API.Controllers
{
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IBirthdayService birthdayService;

        public UserController(IBirthdayService birthdayService)
        {
            this.birthdayService = birthdayService;
        }

        [HttpGet("users/birthday")]
        public async Task<IActionResult> GetBirthday()
        {
            try
            {
                return Ok(await birthdayService.GetBirthdayUsersAsync());
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost("user")]
        public async Task<IActionResult> Create([FromBody] User newUser)
        {
            try
            {
                await birthdayService.CreateUserAsync(newUser);
                return Ok();
            }
            catch (Exception)
            {
                throw;
            }
            }

        [HttpDelete("user/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await birthdayService.DeleteUserAsync(id);
                return Ok();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
