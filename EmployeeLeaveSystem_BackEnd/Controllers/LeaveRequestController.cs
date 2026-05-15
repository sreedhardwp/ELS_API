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

        // GET api/leaverequest/myleave
        [HttpGet("myleave")]
        public async Task<IActionResult> GetMyLeaves()
        {
            try
            {
                var employeeId = GetEmployeeId();

                if (employeeId <= 0)
                    return Unauthorized("Invalid employee token.");

                var leaves = await _leaveService.GetMyLeavesAsync(employeeId);

                if (leaves == null || !leaves.Any())
                    return NotFound("No leave records found.");

                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while fetching leave records.",
                    Error = ex.Message
                });
            }
        }

        // GET api/leaverequest/pending
        [HttpGet("pending")]
        [Authorize(Policy = "ManagerOrHR")]
        public async Task<IActionResult> GetPendingLeaves()
        {
            try
            {
                var leaves = await _leaveService.GetPendingLeavesAsync();

                if (leaves == null || !leaves.Any())
                    return NotFound("No pending leave requests found.");

                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while fetching pending leaves.",
                    Error = ex.Message
                });
            }
        }

        // POST api/leaverequest/apply
        [HttpPost("apply")]
        [Authorize(Policy = "EmployeeOnly")]
        public async Task<IActionResult> ApplyLeave([FromBody] CreateLeaveRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var employeeId = GetEmployeeId();

                if (employeeId <= 0)
                    return Unauthorized("Invalid employee token.");

                var result = await _leaveService.ApplyLeaveAsync(dto, employeeId);

                if (!result)
                    return BadRequest("Insufficient leave balance or invalid request.");

                return Ok("Leave applied successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while applying leave.",
                    Error = ex.Message
                });
            }
        }

        // DELETE api/leaverequest/cancel/id
        [HttpDelete("cancel/{id}")]
        [Authorize(Policy = "EmployeeOnly")]
        public async Task<IActionResult> CancelLeave(int id)
        {
            try
            {
                var employeeId = GetEmployeeId();

                if (employeeId <= 0)
                    return Unauthorized("Invalid employee token.");

                var result = await _leaveService.CancelLeaveAsync(id, employeeId);

                if (!result)
                    return BadRequest("Cannot cancel. Leave not found or already processed.");

                return Ok("Leave cancelled successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while cancelling leave.",
                    Error = ex.Message
                });
            }
        }

        private int GetEmployeeId()
        {
            try
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(claim))
                    return 0;

                return int.Parse(claim);
            }
            catch
            {
                return 0;
            }
        }
    }
}