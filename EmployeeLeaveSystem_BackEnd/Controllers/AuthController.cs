using EmployeeLeaveSystem_BackEnd.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static EmployeeLeaveSystem_BackEnd.DTOs.AuthDto;

namespace EmployeeLeaveSystem_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _authService.RegisterAsync(dto);

                if (!result)
                    return BadRequest("Email already exists.");

                return Ok("Registered successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred during registration.",
                    Error = ex.Message
                });
            }
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var token = await _authService.LoginAsync(dto);

                if (token == null)
                    return Unauthorized("Invalid email or password.");

                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var role = jwtToken.Claims
                    .FirstOrDefault(c => c.Type ==
                        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                    ?.Value;

                var name = jwtToken.Claims
                    .FirstOrDefault(c => c.Type ==
                        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
                    ?.Value;

                return Ok(new
                {
                    token,
                    role,
                    name
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred during login.",
                    Error = ex.Message
                });
            }
        }
    }
}