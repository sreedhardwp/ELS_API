using EmployeeLeaveSystem_BackEnd.Data;
using EmployeeLeaveSystem_BackEnd.DTOs;
using EmployeeLeaveSystem_BackEnd.Models;
using EmployeeLeaveSystem_BackEnd.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static EmployeeLeaveSystem_BackEnd.DTOs.AuthDto;

namespace EmployeeLeaveSystem_BackEnd.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<string?> LoginAsync(LoginDto dto)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == dto.Email);

            if (employee == null) return null;
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, employee.PasswordHash)) return null;

            return GenerateToken(employee);
        }

        public async Task<bool> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Employees.AnyAsync(e => e.Email == dto.Email))
                return false;

            var employee = new Employee
            {
                Name       = dto.Name,
                Email      = dto.Email,
                Department = dto.Department,
                Role       = dto.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateToken(Employee employee)
        {
            var jwt     = _config.GetSection("JwtSettings");
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
                new Claim(ClaimTypes.Email,          employee.Email),
                new Claim(ClaimTypes.Role,           employee.Role),
                new Claim(ClaimTypes.Name,           employee.Name)
            };

            var token = new JwtSecurityToken(
                issuer:             jwt["Issuer"],
                audience:           jwt["Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpiryMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}