using System.ComponentModel.DataAnnotations;

namespace EmployeeLeaveSystem_BackEnd.Models
{
    public class Employee
    {

            [Key]
            public int EmployeeId { get; set; }

            [Required, MaxLength(100)]
            public string Name { get; set; } = string.Empty;

            [Required, MaxLength(150)]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string PasswordHash { get; set; } = string.Empty;

            [MaxLength(100)]
            public string Department { get; set; } = string.Empty;

            // Role: "Employee", "Manager", "HRAdmin"
            [Required]
            public string Role { get; set; } = "Employee";

            public int LeaveBalance { get; set; } = 20;

            // Navigation
            public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
            public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    }
}

