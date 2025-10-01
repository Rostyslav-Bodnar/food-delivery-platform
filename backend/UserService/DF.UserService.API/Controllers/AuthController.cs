using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DF.UserService.Application.Interfaces;
using DF.UserService.Contracts.Models.Request;

namespace DF.UserService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService userService;

        public AuthController(IUserService userService)
        {
            this.userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await userService.RegisterAsync(request);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await userService.LoginAsync(request);
            return Ok(new { Message = result });
        }
        
    }
}
