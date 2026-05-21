using System;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EmployeeLeaveSystem_BackEnd.Controllers;
using EmployeeLeaveSystem_BackEnd.DTOs;
using EmployeeLeaveSystem_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EmployeeLeaveSystem_Tests.ControllerTests
{
    public class AuthControllerTests
    {
        private class FakeAuthService : IAuthService
        {
            private readonly Func<AuthDto.LoginDto, Task<string?>> _login;
            private readonly Func<AuthDto.RegisterDto, Task<bool>> _register;

            public FakeAuthService(Func<AuthDto.LoginDto, Task<string?>> login, Func<AuthDto.RegisterDto, Task<bool>> register)
            {
                _login = login;
                _register = register;
            }

            public Task<string?> LoginAsync(AuthDto.LoginDto dto) => _login(dto);

            public Task<bool> RegisterAsync(AuthDto.RegisterDto dto) => _register(dto);
        }

        [Fact]
        public async Task Register_ModelStateInvalid_ReturnsBadRequest()
        {
            var svc = new FakeAuthService(_ => Task.FromResult<string?>(null), _ => Task.FromResult(true));
            var controller = new AuthController(svc);
            controller.ModelState.AddModelError("Email", "Required");

            var dto = new AuthDto.RegisterDto();
            var result = await controller.Register(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_EmailExists_ReturnsBadRequest()
        {
            var svc = new FakeAuthService(_ => Task.FromResult<string?>(null), _ => Task.FromResult(false));
            var controller = new AuthController(svc);

            var dto = new AuthDto.RegisterDto { Email = "a@b.com", Name = "n", Password = "p" };
            var result = await controller.Register(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Email already exists", bad.Value?.ToString());
        }

        [Fact]
        public async Task Register_Success_ReturnsOk()
        {
            var svc = new FakeAuthService(_ => Task.FromResult<string?>(null), _ => Task.FromResult(true));
            var controller = new AuthController(svc);

            var dto = new AuthDto.RegisterDto { Email = "ok@ok.com", Name = "n", Password = "p" };
            var result = await controller.Register(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Registered successfully", ok.Value?.ToString());
        }

        [Fact]
        public async Task Login_ModelStateInvalid_ReturnsBadRequest()
        {
            var svc = new FakeAuthService(_ => Task.FromResult<string?>(null), _ => Task.FromResult(true));
            var controller = new AuthController(svc);
            controller.ModelState.AddModelError("Email", "Required");

            var dto = new AuthDto.LoginDto();
            var result = await controller.Login(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var svc = new FakeAuthService(_ => Task.FromResult<string?>(null), _ => Task.FromResult(true));
            var controller = new AuthController(svc);

            var dto = new AuthDto.LoginDto { Email = "x", Password = "y" };
            var result = await controller.Login(dto);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Invalid email or password", unauthorized.Value?.ToString());
        }

        [Fact]
        public async Task Login_Valid_ReturnsOkWithTokenRoleName()
        {
            // prepare a JWT token string with the expected claims
            var roleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
            var nameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
            var roleValue = "Manager";
            var nameValue = "Alice";

            var claims = new[] { new Claim(roleClaimType, roleValue), new Claim(nameClaimType, nameValue) };
            var token = new JwtSecurityToken(claims: claims);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var svc = new FakeAuthService(_ => Task.FromResult<string?>(tokenString), _ => Task.FromResult(true));
            var controller = new AuthController(svc);

            var dto = new AuthDto.LoginDto { Email = "a@b.com", Password = "p" };
            var result = await controller.Login(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var value = ok.Value ?? throw new InvalidOperationException("Value expected");

            // Use reflection to read anonymous object properties
            var t = value.GetType();
            var tokenProp = t.GetProperty("token");
            var roleProp = t.GetProperty("role");
            var nameProp = t.GetProperty("name");

            Assert.NotNull(tokenProp);
            Assert.NotNull(roleProp);
            Assert.NotNull(nameProp);

            Assert.Equal(tokenString, tokenProp.GetValue(value));
            Assert.Equal(roleValue, roleProp.GetValue(value));
            Assert.Equal(nameValue, nameProp.GetValue(value));
        }
    }
}
