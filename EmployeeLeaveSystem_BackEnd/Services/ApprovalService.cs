using EmployeeLeaveSystem_BackEnd.Data;
using EmployeeLeaveSystem_BackEnd.DTOs;
using EmployeeLeaveSystem_BackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeLeaveSystem_BackEnd.Services
{
    public class ApprovalService : IApprovalService
    {
        private readonly AppDbContext _context;

        public ApprovalService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> DecideAsync(
            ApprovalDto.ApprovalRequestDto dto,
            ClaimsPrincipal user)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .FirstOrDefaultAsync(lr =>
                        lr.LeaveRequestId == dto.LeaveRequestId &&
                        lr.Status == "Pending");

                if (leaveRequest == null)
                    return new NotFoundObjectResult("Leave request not found or already processed.");

                if (dto.Decision != "Approved" && dto.Decision != "Rejected")
                    return new BadRequestObjectResult("Decision must be 'Approved' or 'Rejected'.");

                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return new UnauthorizedObjectResult("Invalid user token.");

                int managerId = int.Parse(userIdClaim.Value);

                var approval = new Approval
                {
                    LeaveRequestId = dto.LeaveRequestId,
                    ManagerId = managerId,
                    Decision = dto.Decision,
                    Remarks = dto.Remarks,
                    DecisionDate = DateTime.UtcNow
                };

                leaveRequest.Status = dto.Decision;

                if (dto.Decision == "Approved")
                {
                    int days = (leaveRequest.EndDate - leaveRequest.StartDate).Days + 1;

                    if (leaveRequest.Employee.LeaveBalance < days)
                        return new BadRequestObjectResult("Insufficient leave balance.");

                    leaveRequest.Employee.LeaveBalance -= days;
                }

                _context.Approvals.Add(approval);
                await _context.SaveChangesAsync();

                return new OkObjectResult($"Leave request {dto.Decision} successfully.");
            }
            catch (DbUpdateException ex)
            {
                return new ObjectResult(new
                {
                    Message = "Database update failed.",
                    Error = ex.Message
                })
                { StatusCode = 500 };
            }
            catch (Exception ex)
            {
                return new ObjectResult(new
                {
                    Message = "An unexpected error occurred.",
                    Error = ex.Message
                })
                { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetHistoryAsync()
        {
            try
            {
                var history = await _context.Approvals
                    .Include(a => a.LeaveRequest)
                        .ThenInclude(lr => lr.Employee)
                    .Include(a => a.Manager)
                    .Select(a => new ApprovalDto.ApprovalResponseDto
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

                if (!history.Any())
                    return new NotFoundObjectResult("No approval history found.");

                return new OkObjectResult(history);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new
                {
                    Message = "An error occurred while fetching approval history.",
                    Error = ex.Message
                })
                { StatusCode = 500 };
            }
        }
    }
}