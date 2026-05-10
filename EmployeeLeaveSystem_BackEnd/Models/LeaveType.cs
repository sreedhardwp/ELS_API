using EmployeeLeaveSystem_BackEnd.Models;
using System.ComponentModel.DataAnnotations;

namespace EmployeeLeaveSystem_BackEnd.Models
{
    public class LeaveType
    {
        [Key]
        public int LeaveTypeId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty; // Annual, Sick, Casual, Maternity

        public int MaxDaysAllowed { get; set; } = 10;

        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    }
}
