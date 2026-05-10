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

        // POST api/approval/decide
        [HttpPost("decide")]
        public async Task<IActionResult> Decide([FromBody] ApprovalRequestDto dto)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .FirstOrDefaultAsync(lr => lr.LeaveRequestId == dto.LeaveRequestId
                                        && lr.Status == "Pending");

            if (leaveRequest == null)
                return NotFound("Leave request not found or already processed.");

            if (dto.Decision != "Approved" && dto.Decision != "Rejected")
                return BadRequest("Decision must be 'Approved' or 'Rejected'.");

            var managerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

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
                leaveRequest.Employee.LeaveBalance -= days;
            }

            _context.Approvals.Add(approval);
            await _context.SaveChangesAsync();

            return Ok($"Leave request {dto.Decision} successfully.");
        }

        // GET api/approval/history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var history = await _context.Approvals
                .Include(a => a.LeaveRequest).ThenInclude(lr => lr.Employee)
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

            return Ok(history);
        }
    }
}