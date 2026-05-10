using EmployeeLeaveSystem_BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeLeaveSystem_BackEnd.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Approval> Approvals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Unique email
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            // One LeaveRequest → One Approval
            modelBuilder.Entity<Approval>()
                .HasOne(a => a.LeaveRequest)
                .WithOne(lr => lr.Approval)
                .HasForeignKey<Approval>(a => a.LeaveRequestId);

            // Manager FK (Employee table self-reference)
            modelBuilder.Entity<Approval>()
                .HasOne(a => a.Manager)
                .WithMany(e => e.Approvals)
                .HasForeignKey(a => a.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed LeaveTypes
            modelBuilder.Entity<LeaveType>().HasData(
                new LeaveType { LeaveTypeId = 1, Name = "Annual", MaxDaysAllowed = 15 },
                new LeaveType { LeaveTypeId = 2, Name = "Sick", MaxDaysAllowed = 10 },
                new LeaveType { LeaveTypeId = 3, Name = "Casual", MaxDaysAllowed = 7 },
                new LeaveType { LeaveTypeId = 4, Name = "Maternity", MaxDaysAllowed = 90 }
            );
        }
    }
}


       
