namespace EmployeeLeaveSystem_BackEnd.DTOs
{
    public class ReportDto
    {
        public class ApprovalRequestDto
        {
            public int LeaveRequestId { get; set; }
            public string Decision { get; set; } = string.Empty; // "Approved" or "Rejected"
            public string? Remarks { get; set; }
        }

        public class ApprovalResponseDto
        {
            public int ApprovalId { get; set; }
            public int LeaveRequestId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public string ManagerName { get; set; } = string.Empty;
            public string Decision { get; set; } = string.Empty;
            public string? Remarks { get; set; }
            public DateTime DecisionDate { get; set; }
        }
    }
}
