using EmployeeLeaveSystem_BackEnd.DTOs;
using EmployeeLeaveSystem_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeLeaveSystem_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ManagerOrHR")]
    public class ApprovalController : ControllerBase
    {
        private readonly IApprovalService _approvalService;

        public ApprovalController(IApprovalService approvalService)
        {
            _approvalService = approvalService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> Decide(
            [FromBody] ApprovalDto.ApprovalRequestDto dto)
        {
            try
            {
                return await _approvalService.DecideAsync(dto, User);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while processing the approval.",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                return await _approvalService.GetHistoryAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while fetching approval history.",
                    Error = ex.Message
                });
            }
        }
    }
}