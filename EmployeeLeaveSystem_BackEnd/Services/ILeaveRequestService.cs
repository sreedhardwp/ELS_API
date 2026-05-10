using static EmployeeLeaveSystem_BackEnd.DTOs.LeaveRequestDto;

namespace EmployeeLeaveSystem_BackEnd.Services
{
    public interface ILeaveRequestService
    {
        Task<bool> ApplyLeaveAsync(CreateLeaveRequestDto dto, int employeeId);
        Task<List<LeaveRequestResponseDto>> GetMyLeavesAsync(int employeeId);
        Task<List<LeaveRequestResponseDto>> GetPendingLeavesAsync();
        Task<bool> CancelLeaveAsync(int leaveRequestId, int employeeId);
    }
}
