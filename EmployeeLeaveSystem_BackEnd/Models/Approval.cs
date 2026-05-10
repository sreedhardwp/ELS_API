using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeLeaveSystem_BackEnd.Models
{
    public class Approval
    {
            [Key]
            public int ApprovalId { get; set; }

            [ForeignKey("LeaveRequest")]
            public int LeaveRequestId { get; set; }
            public LeaveRequest LeaveRequest { get; set; } = null!;

            // The manager who approved/rejected
            [ForeignKey("Manager")]
            public int ManagerId { get; set; }
            public Employee Manager { get; set; } = null!;

            // Approved or Rejected
            [Required]
            public string Decision { get; set; } = string.Empty;

            [MaxLength(500)]
            public string? Remarks { get; set; }

            public DateTime DecisionDate { get; set; } = DateTime.UtcNow;
    }
}
