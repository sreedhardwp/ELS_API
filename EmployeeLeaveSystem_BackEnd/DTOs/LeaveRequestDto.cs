namespace EmployeeLeaveSystem_BackEnd.DTOs
{
    public class LeaveRequestDto
    {
        public class CreateLeaveRequestDto
        {
            public int LeaveTypeId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        public class LeaveRequestResponseDto
        {
            public int LeaveRequestId { get; set; }
            public string? EmployeeName { get; set; }
            public string LeaveTypeName { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Reason { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime RequestedOn { get; set; }
            public string? Remarks { get; set; }
        }
    }
}
