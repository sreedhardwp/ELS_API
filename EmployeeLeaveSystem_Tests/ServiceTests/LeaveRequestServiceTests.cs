using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeLeaveSystem_BackEnd.Data;
using EmployeeLeaveSystem_BackEnd.DTOs;
using EmployeeLeaveSystem_BackEnd.Models;
using EmployeeLeaveSystem_BackEnd.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmployeeLeaveSystem_Tests.ServiceTests
{
    public class LeaveRequestServiceTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task ApplyLeaveAsync_ReturnsFalse_WhenEmployeeNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var svc = new LeaveRequestService(context);

            var dto = new LeaveRequestDto.CreateLeaveRequestDto { LeaveTypeId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow };
            var result = await svc.ApplyLeaveAsync(dto, employeeId: 999);

            Assert.False(result);
        }

        [Fact]
        public async Task ApplyLeaveAsync_ReturnsFalse_WhenLeaveTypeNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            context.Employees.Add(new Employee { EmployeeId = 1, Name = "E", Email = "e@x.com", PasswordHash = "x" });
            await context.SaveChangesAsync();

            var svc = new LeaveRequestService(context);
            var dto = new LeaveRequestDto.CreateLeaveRequestDto { LeaveTypeId = 42, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow };
            var result = await svc.ApplyLeaveAsync(dto, employeeId: 1);

            Assert.False(result);
        }

        [Fact]
        public async Task ApplyLeaveAsync_ReturnsFalse_WhenDaysExceedMax()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            context.Employees.Add(new Employee { EmployeeId = 2, Name = "E2", Email = "e2@x.com", PasswordHash = "x" });
            context.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual", MaxDaysAllowed = 2 });
            await context.SaveChangesAsync();

            var svc = new LeaveRequestService(context);
            var dto = new LeaveRequestDto.CreateLeaveRequestDto { LeaveTypeId = 1, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(3) };
            var result = await svc.ApplyLeaveAsync(dto, employeeId: 2);

            Assert.False(result);
        }

        [Fact]
        public async Task ApplyLeaveAsync_ReturnsTrue_AndSavesLeave_WhenValid()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            context.Employees.Add(new Employee { EmployeeId = 3, Name = "E3", Email = "e3@x.com", PasswordHash = "x" });
            context.LeaveTypes.Add(new LeaveType { LeaveTypeId = 2, Name = "Sick", MaxDaysAllowed = 10 });
            await context.SaveChangesAsync();

            var svc = new LeaveRequestService(context);
            var dto = new LeaveRequestDto.CreateLeaveRequestDto { LeaveTypeId = 2, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(2), Reason = "R" };
            var result = await svc.ApplyLeaveAsync(dto, employeeId: 3);

            Assert.True(result);

            var saved = await context.LeaveRequests.Include(l => l.LeaveType).FirstOrDefaultAsync(l => l.EmployeeId == 3);
            Assert.NotNull(saved);
            Assert.Equal(dto.Reason, saved.Reason);
            Assert.Equal("Pending", saved.Status);
            Assert.Equal(2, saved.LeaveTypeId);
        }

        [Fact]
        public async Task GetMyLeavesAsync_ReturnsMappedResults()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var lt = new LeaveType { LeaveTypeId = 5, Name = "Casual", MaxDaysAllowed = 7 };
            context.LeaveTypes.Add(lt);
            context.Employees.Add(new Employee { EmployeeId = 4, Name = "Emp4", Email = "4@x.com", PasswordHash = "x" });

            var lr = new LeaveRequest
            {
                LeaveRequestId = 11,
                EmployeeId = 4,
                LeaveTypeId = lt.LeaveTypeId,
                LeaveType = lt,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
                Reason = "R",
                Status = "Pending",
                RequestedOn = DateTime.UtcNow
            };
            context.LeaveRequests.Add(lr);
            await context.SaveChangesAsync();

            var svc = new LeaveRequestService(context);
            var res = await svc.GetMyLeavesAsync(4);

            Assert.Single(res);
            var item = res.First();
            Assert.Equal(lr.LeaveRequestId, item.LeaveRequestId);
            Assert.Equal(lt.Name, item.LeaveTypeName);
            Assert.Equal(lr.Reason, item.Reason);
        }

        [Fact]
        public async Task GetPendingLeavesAsync_ReturnsOnlyPending()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var lt = new LeaveType { LeaveTypeId = 6, Name = "L6", MaxDaysAllowed = 5 };
            context.LeaveTypes.Add(lt);
            context.Employees.Add(new Employee { EmployeeId = 6, Name = "Emp6", Email = "6@x.com", PasswordHash = "x" });

            context.LeaveRequests.Add(new LeaveRequest { LeaveRequestId = 21, EmployeeId = 6, LeaveTypeId = lt.LeaveTypeId, LeaveType = lt, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow, Status = "Pending", RequestedOn = DateTime.UtcNow });
            context.LeaveRequests.Add(new LeaveRequest { LeaveRequestId = 22, EmployeeId = 6, LeaveTypeId = lt.LeaveTypeId, LeaveType = lt, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow, Status = "Approved", RequestedOn = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var svc = new LeaveRequestService(context);
            var res = await svc.GetPendingLeavesAsync();

            Assert.Single(res);
            Assert.Equal("Pending", res.First().Status);
        }

        [Fact]
        public async Task CancelLeaveAsync_ReturnsFalse_WhenNotFoundOrNotPending()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            context.Employees.Add(new Employee { EmployeeId = 7, Name = "E7", Email = "7@x.com", PasswordHash = "x" });
            context.LeaveRequests.Add(new LeaveRequest { LeaveRequestId = 31, EmployeeId = 7, LeaveTypeId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow, Status = "Approved" });
            await context.SaveChangesAsync();

            var svc = new LeaveRequestService(context);
            var res = await svc.CancelLeaveAsync(31, 7);

            Assert.False(res);
        }

        [Fact]
        public async Task CancelLeaveAsync_ReturnsTrue_WhenPending()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            context.Employees.Add(new Employee { EmployeeId = 8, Name = "E8", Email = "8@x.com", PasswordHash = "x" });
            context.LeaveRequests.Add(new LeaveRequest { LeaveRequestId = 32, EmployeeId = 8, LeaveTypeId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow, Status = "Pending" });
            await context.SaveChangesAsync();

            var svc = new LeaveRequestService(context);
            var res = await svc.CancelLeaveAsync(32, 8);

            Assert.True(res);
            var exists = await context.LeaveRequests.FindAsync(32);
            Assert.Null(exists);
        }
    }
}
