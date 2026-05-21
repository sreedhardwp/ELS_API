using System;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmployeeLeaveSystem_BackEnd.Data;
using EmployeeLeaveSystem_BackEnd.DTOs;
using EmployeeLeaveSystem_BackEnd.Models;
using EmployeeLeaveSystem_BackEnd.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EmployeeLeaveSystem_Tests.ServiceTests
{
    public class AuthServiceTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new AppDbContext(options);
        }

        private static IConfiguration CreateConfiguration()
        {
            var dict = new Dictionary<string, string?>
            {
                { "JwtSettings:SecretKey", "ThisIsASecretKeyForTesting1234567890123456" },
                { "JwtSettings:Issuer", "test-issuer" },
                { "JwtSettings:Audience", "test-audience" },
                { "JwtSettings:ExpiryMinutes", "60" }
            };

            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();

            return cfg;
        }

        [Fact]
        public async Task RegisterAsync_ReturnsTrueAndSavesEmployee_WhenNewEmail()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var config = CreateConfiguration();
            var service = new AuthService(context, config);

            var dto = new AuthDto.RegisterDto
            {
                Name = "Test",
                Email = "test@example.com",
                Password = "Password123!",
                Department = "Dev",
                Role = "Employee"
            };

            var result = await service.RegisterAsync(dto);

            Assert.True(result);

            var saved = await context.Employees.FirstOrDefaultAsync(e => e.Email == dto.Email);
            Assert.NotNull(saved);
            Assert.Equal(dto.Name, saved.Name);
            Assert.Equal(dto.Email, saved.Email);
            Assert.Equal(dto.Department, saved.Department);
            Assert.Equal(dto.Role, saved.Role);
            Assert.NotEqual(dto.Password, saved.PasswordHash); // should be hashed
            Assert.True(BCrypt.Net.BCrypt.Verify(dto.Password, saved.PasswordHash));
        }

        [Fact]
        public async Task RegisterAsync_ReturnsFalse_WhenEmailExists()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var config = CreateConfiguration();

            context.Employees.Add(new Employee
            {
                Name = "Existing",
                Email = "exist@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("x"),
                Role = "Employee",
                Department = "D"
            });
            await context.SaveChangesAsync();

            var service = new AuthService(context, config);

            var dto = new AuthDto.RegisterDto
            {
                Name = "New",
                Email = "exist@example.com",
                Password = "Password123",
                Department = "D",
                Role = "Employee"
            };

            var result = await service.RegisterAsync(dto);
            Assert.False(result);
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenUserNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var config = CreateConfiguration();
            var service = new AuthService(context, config);

            var dto = new AuthDto.LoginDto { Email = "noone@example.com", Password = "p" };
            var token = await service.LoginAsync(dto);
            Assert.Null(token);
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenPasswordIncorrect()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var config = CreateConfiguration();

            var emp = new Employee
            {
                Name = "U",
                Email = "u@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
                Role = "Employee",
                Department = "D"
            };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();

            var service = new AuthService(context, config);
            var dto = new AuthDto.LoginDto { Email = emp.Email, Password = "Wrong" };
            var token = await service.LoginAsync(dto);
            Assert.Null(token);
        }

        [Fact]
        public async Task LoginAsync_ReturnsToken_WhenCredentialsValid()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var config = CreateConfiguration();

            var emp = new Employee
            {
                Name = "U2",
                Email = "u2@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Secret123"),
                Role = "Manager",
                Department = "D"
            };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();

            var service = new AuthService(context, config);
            var dto = new AuthDto.LoginDto { Email = emp.Email, Password = "Secret123" };
            var token = await service.LoginAsync(dto);

            Assert.NotNull(token);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token!);

            var role = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            var name = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;
            var id = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            Assert.Equal(emp.Role, role);
            Assert.Equal(emp.Name, name);
            Assert.Equal(emp.EmployeeId.ToString(), id);
        }
    }
}
