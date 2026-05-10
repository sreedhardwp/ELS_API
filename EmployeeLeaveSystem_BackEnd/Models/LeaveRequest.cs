using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeLeaveSystem_BackEnd.Models
{
    public class LeaveRequest
    {
            [Key]
            public int LeaveRequestId { get; set; }

            [ForeignKey("Employee")]
            public int EmployeeId { get; set; }
            public Employee Employee { get; set; } = null!;

            [ForeignKey("LeaveType")]
            public int LeaveTypeId { get; set; }
            public LeaveType LeaveType { get; set; } = null!;

            [Required]
            public DateTime StartDate { get; set; }

            [Required]
            public DateTime EndDate { get; set; }

            [MaxLength(500)]
            public string Reason { get; set; } = string.Empty;

            // Pending, Approved, Rejected
            public string Status { get; set; } = "Pending";

            public DateTime RequestedOn { get; set; } = DateTime.UtcNow;

            public Approval? Approval { get; set; }
        
    }
}

