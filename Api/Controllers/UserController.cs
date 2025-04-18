using Microsoft.AspNetCore.Mvc;
using api.Models;
using api.Services;

namespace MyApp.Namespace
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService userService = new();

        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            if (userService.UserExists(user.Email)) return Conflict("Email already in use");
            userService.RegisterUser(user);
            return Ok("Registration successful");
        }
        
        [HttpPost("login")]
        public IActionResult Login([FromBody] User user)
        {
            var matchedUser = userService.LoginUser(user.Email, user.Password);
            if (matchedUser == null) return Unauthorized("Invalid credentials");

            return Ok(matchedUser);
        }

    }
}
