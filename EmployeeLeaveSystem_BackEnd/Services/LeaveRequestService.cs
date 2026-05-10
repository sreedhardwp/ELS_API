using EmployeeLeaveSystem_BackEnd.Data;
using EmployeeLeaveSystem_BackEnd.DTOs;
using EmployeeLeaveSystem_BackEnd.Models;
using Microsoft.EntityFrameworkCore;
using static EmployeeLeaveSystem_BackEnd.DTOs.LeaveRequestDto;

namespace EmployeeLeaveSystem_BackEnd.Services
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly AppDbContext _context;

        public LeaveRequestService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ApplyLeaveAsync(CreateLeaveRequestDto dto, int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return false;

            int days = (dto.EndDate - dto.StartDate).Days + 1;
            if (employee.LeaveBalance < days) return false;

            var leaveRequest = new LeaveRequest
            {
                EmployeeId = employeeId,
                LeaveTypeId = dto.LeaveTypeId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Reason = dto.Reason,
                Status = "Pending",
                RequestedOn = DateTime.UtcNow
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<LeaveRequestResponseDto>> GetMyLeavesAsync(int employeeId)
        {
            return await _context.LeaveRequests
                .Where(lr => lr.EmployeeId == employeeId)
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.Approval)
                .Select(lr => new LeaveRequestResponseDto
                {
                    LeaveRequestId = lr.LeaveRequestId,
                    LeaveTypeName = lr.LeaveType.Name,
                    StartDate = lr.StartDate,
                    EndDate = lr.EndDate,
                    Reason = lr.Reason,
                    Status = lr.Status,
                    RequestedOn = lr.RequestedOn,
                    Remarks = lr.Approval != null ? lr.Approval.Remarks : null
                })
                .ToListAsync();
        }

        public async Task<List<LeaveRequestResponseDto>> GetPendingLeavesAsync()
        {
            return await _context.LeaveRequests
                .Where(lr => lr.Status == "Pending")
                .Include(lr => lr.Employee)
                .Include(lr => lr.LeaveType)
                .Select(lr => new LeaveRequestResponseDto
                {
                    LeaveRequestId = lr.LeaveRequestId,
                    EmployeeName = lr.Employee.Name,
                    LeaveTypeName = lr.LeaveType.Name,
                    StartDate = lr.StartDate,
                    EndDate = lr.EndDate,
                    Reason = lr.Reason,
                    Status = lr.Status,
                    RequestedOn = lr.RequestedOn
                })
                .ToListAsync();
        }

        public async Task<bool> CancelLeaveAsync(int leaveRequestId, int employeeId)
        {
            var leave = await _context.LeaveRequests
                .FirstOrDefaultAsync(lr => lr.LeaveRequestId == leaveRequestId
                                        && lr.EmployeeId == employeeId
                                        && lr.Status == "Pending");

            if (leave == null) return false;

            _context.LeaveRequests.Remove(leave);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}