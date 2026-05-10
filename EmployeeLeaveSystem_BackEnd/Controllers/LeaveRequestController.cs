using EmployeeLeaveSystem_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static EmployeeLeaveSystem_BackEnd.DTOs.LeaveRequestDto;

namespace EmployeeLeaveSystem_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaveRequestController : ControllerBase
    {
        private readonly ILeaveRequestService _leaveService;

        public LeaveRequestController(ILeaveRequestService leaveService)
        {
            _leaveService = leaveService;
        }

        // GET api/leaverequest/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMyLeaves()
        {
            var employeeId = GetEmployeeId();
            var leaves = await _leaveService.GetMyLeavesAsync(employeeId);
            return Ok(leaves);
        }

        // GET api/leaverequest/pending  (Manager/HR only)
        [HttpGet("pending")]
        [Authorize(Policy = "ManagerOrHR")]
        public async Task<IActionResult> GetPendingLeaves()
        {
            var leaves = await _leaveService.GetPendingLeavesAsync();
            return Ok(leaves);
        }

        // POST api/leaverequest/apply
        [HttpPost("apply")]
        [Authorize(Policy = "EmployeeOnly")]
        public async Task<IActionResult> ApplyLeave([FromBody] CreateLeaveRequestDto dto)
        {
            var employeeId = GetEmployeeId();
            var result = await _leaveService.ApplyLeaveAsync(dto, employeeId);
            if (!result)
                return BadRequest("Insufficient leave balance or invalid request.");

            return Ok("Leave applied successfully.");
        }

        // DELETE api/leaverequest/cancel/5
        [HttpDelete("cancel/{id}")]
        [Authorize(Policy = "EmployeeOnly")]
        public async Task<IActionResult> CancelLeave(int id)
        {
            var employeeId = GetEmployeeId();
            var result = await _leaveService.CancelLeaveAsync(id, employeeId);
            if (!result)
                return BadRequest("Cannot cancel. Leave not found or already processed.");

            return Ok("Leave cancelled successfully.");
        }

        // Helper — reads EmployeeId from JWT token
        private int GetEmployeeId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(claim!);
        }
    }
}
