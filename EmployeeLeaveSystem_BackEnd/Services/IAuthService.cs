using EmployeeLeaveSystem_BackEnd.DTOs;
using static EmployeeLeaveSystem_BackEnd.DTOs.AuthDto;

namespace EmployeeLeaveSystem_BackEnd.Services
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(LoginDto dto);
        Task<bool> RegisterAsync(RegisterDto dto);
    }
}