using EmployeeLeaveSystem_BackEnd.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmployeeLeaveSystem_BackEnd.Services
{
    public interface IApprovalService
    {
        Task<IActionResult> DecideAsync(
            ApprovalDto.ApprovalRequestDto dto,
            ClaimsPrincipal user);

        Task<IActionResult> GetHistoryAsync();
    }
}