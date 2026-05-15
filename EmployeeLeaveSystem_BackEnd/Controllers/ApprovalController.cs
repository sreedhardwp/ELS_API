using EmployeeLeaveSystem_BackEnd.Data;
using EmployeeLeaveSystem_BackEnd.Models;
using EmployeeLeaveSystem_BackEnd.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using static EmployeeLeaveSystem_BackEnd.DTOs.ApprovalDto;

namespace EmployeeLeaveSystem_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ManagerOrHR")]
    public class ApprovalController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApprovalController(AppDbContext context)
        {
            _context = context;
        }

        // POST api/approval/process
        [HttpPost("process")]
        public async Task<IActionResult> Decide([FromBody] ApprovalRequestDto dto)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .FirstOrDefaultAsync(lr => lr.LeaveRequestId == dto.LeaveRequestId
                                            && lr.Status == "Pending");

                if (leaveRequest == null)
                    return NotFound("Leave request not found or already processed.");

                if (dto.Decision != "Approved" && dto.Decision != "Rejected")
                    return BadRequest("Decision must be 'Approved' or 'Rejected'.");

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized("Invalid user token.");

                var managerId = int.Parse(userIdClaim.Value);

                // Create approval record
                var approval = new Approval
                {
                    LeaveRequestId = dto.LeaveRequestId,
                    ManagerId = managerId,
                    Decision = dto.Decision,
                    Remarks = dto.Remarks,
                    DecisionDate = DateTime.UtcNow
                };

                // Update leave status
                leaveRequest.Status = dto.Decision;

                // Deduct balance if approved
                if (dto.Decision == "Approved")
                {
                    int days = (leaveRequest.EndDate - leaveRequest.StartDate).Days + 1;

                    if (leaveRequest.Employee.LeaveBalance < days)
                        return BadRequest("Insufficient leave balance.");

                    leaveRequest.Employee.LeaveBalance -= days;
                }

                _context.Approvals.Add(approval);

                await _context.SaveChangesAsync();

                return Ok($"Leave request {dto.Decision} successfully.");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new
                {
                    Message = "Database update failed.",
                    Error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred.",
                    Error = ex.Message
                });
            }
        }

        // GET api/approval/history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                var history = await _context.Approvals
                    .Include(a => a.LeaveRequest)
                        .ThenInclude(lr => lr.Employee)
                    .Include(a => a.Manager)
                    .Select(a => new ApprovalResponseDto
                    {
                        ApprovalId = a.ApprovalId,
                        LeaveRequestId = a.LeaveRequestId,
                        EmployeeName = a.LeaveRequest.Employee.Name,
                        ManagerName = a.Manager.Name,
                        Decision = a.Decision,
                        Remarks = a.Remarks,
                        DecisionDate = a.DecisionDate
                    })
                    .ToListAsync();

                if (history == null || !history.Any())
                    return NotFound("No approval history found.");

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while fetching approval history.",
                    Error = ex.Message
                });
            }
        }
    }
}