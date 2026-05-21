using System;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EmployeeLeaveSystem_BackEnd.Controllers;
using EmployeeLeaveSystem_BackEnd.Data;
using EmployeeLeaveSystem_BackEnd.DTOs;
using EmployeeLeaveSystem_BackEnd.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmployeeLeaveSystem_Tests.ControllerTests
{
    public class ApprovalControllerTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            var context = new AppDbContext(options);
            return context;
        }

        private static ApprovalController CreateControllerWithUser(AppDbContext context, int? userId = null)
        {
            var controller = new ApprovalController(context);

            var httpContext = new DefaultHttpContext();
            if (userId.HasValue)
            {
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            }
            else
            {
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        [Fact]
        public async Task Decide_Approve_SucceedsAndReducesBalance()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);

            // Seed employee, manager, and leave request
            var employee = new Employee { EmployeeId = 1, Name = "Emp", Email = "emp@test.local", PasswordHash = "x", Role = "Employee", LeaveBalance = 10 };
            var manager = new Employee { EmployeeId = 2, Name = "Mgr", Email = "mgr@test.local", PasswordHash = "x", Role = "Manager", LeaveBalance = 20 };
            context.Employees.AddRange(employee, manager);

            var leaveRequest = new LeaveRequest
            {
                LeaveRequestId = 1,
                EmployeeId = employee.EmployeeId,
                Employee = employee,
                LeaveTypeId = 1,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(2), // 3 days
                Status = "Pending"
            };

            context.LeaveRequests.Add(leaveRequest);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, manager.EmployeeId);

            var dto = new ApprovalDto.ApprovalRequestDto { LeaveRequestId = leaveRequest.LeaveRequestId, Decision = "Approved", Remarks = "OK" };

            var result = await controller.Decide(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Approved", ok.Value?.ToString());

            // Reload entities to check effects
            var lr = await context.LeaveRequests.Include(l => l.Employee).FirstAsync(l => l.LeaveRequestId == leaveRequest.LeaveRequestId);
            Assert.Equal("Approved", lr.Status);
            Assert.Equal(7, lr.Employee.LeaveBalance); // 10 - 3

            var approval = await context.Approvals.FirstOrDefaultAsync(a => a.LeaveRequestId == leaveRequest.LeaveRequestId);
            Assert.NotNull(approval);
            Assert.Equal("Approved", approval.Decision);
            Assert.Equal(manager.EmployeeId, approval.ManagerId);
        }

        [Fact]
        public async Task Decide_InsufficientBalance_ReturnsBadRequest()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);

            var employee = new Employee { EmployeeId = 1, Name = "Emp2", Email = "emp2@test.local", PasswordHash = "x", Role = "Employee", LeaveBalance = 1 };
            var manager = new Employee { EmployeeId = 2, Name = "Mgr2", Email = "mgr2@test.local", PasswordHash = "x", Role = "Manager", LeaveBalance = 20 };
            context.Employees.AddRange(employee, manager);

            var leaveRequest = new LeaveRequest
            {
                LeaveRequestId = 2,
                EmployeeId = employee.EmployeeId,
                Employee = employee,
                LeaveTypeId = 1,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(2), // 3 days
                Status = "Pending"
            };

            context.LeaveRequests.Add(leaveRequest);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, manager.EmployeeId);

            var dto = new ApprovalDto.ApprovalRequestDto { LeaveRequestId = leaveRequest.LeaveRequestId, Decision = "Approved", Remarks = "OK" };

            var result = await controller.Decide(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Insufficient leave balance", bad.Value?.ToString());
        }

        [Fact]
        public async Task Decide_InvalidDecision_ReturnsBadRequest()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);

            var employee = new Employee { EmployeeId = 3, Name = "Emp3", Email = "emp3@test.local", PasswordHash = "x", Role = "Employee", LeaveBalance = 10 };
            var manager = new Employee { EmployeeId = 4, Name = "Mgr3", Email = "mgr3@test.local", PasswordHash = "x", Role = "Manager", LeaveBalance = 20 };
            context.Employees.AddRange(employee, manager);

            var leaveRequest = new LeaveRequest
            {
                LeaveRequestId = 3,
                EmployeeId = employee.EmployeeId,
                Employee = employee,
                LeaveTypeId = 1,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
                Status = "Pending"
            };

            context.LeaveRequests.Add(leaveRequest);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, manager.EmployeeId);

            var dto = new ApprovalDto.ApprovalRequestDto { LeaveRequestId = leaveRequest.LeaveRequestId, Decision = "Maybe", Remarks = "?" };

            var result = await controller.Decide(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Decision must be", bad.Value?.ToString());
        }

        [Fact]
        public async Task Decide_NoUserClaim_ReturnsUnauthorized()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);

            var employee = new Employee { EmployeeId = 5, Name = "Emp5", Email = "emp5@test.local", PasswordHash = "x", Role = "Employee", LeaveBalance = 10 };
            context.Employees.Add(employee);

            var leaveRequest = new LeaveRequest
            {
                LeaveRequestId = 5,
                EmployeeId = employee.EmployeeId,
                Employee = employee,
                LeaveTypeId = 1,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
                Status = "Pending"
            };

            context.LeaveRequests.Add(leaveRequest);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, null);

            var dto = new ApprovalDto.ApprovalRequestDto { LeaveRequestId = leaveRequest.LeaveRequestId, Decision = "Approved", Remarks = "OK" };

            var result = await controller.Decide(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Decide_AlreadyProcessed_ReturnsNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);

            var employee = new Employee { EmployeeId = 6, Name = "Emp6", Email = "emp6@test.local", PasswordHash = "x", Role = "Employee", LeaveBalance = 10 };
            var manager = new Employee { EmployeeId = 7, Name = "Mgr6", Email = "mgr6@test.local", PasswordHash = "x", Role = "Manager", LeaveBalance = 20 };
            context.Employees.AddRange(employee, manager);

            var leaveRequest = new LeaveRequest
            {
                LeaveRequestId = 6,
                EmployeeId = employee.EmployeeId,
                Employee = employee,
                LeaveTypeId = 1,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
                Status = "Approved"
            };

            context.LeaveRequests.Add(leaveRequest);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, manager.EmployeeId);

            var dto = new ApprovalDto.ApprovalRequestDto { LeaveRequestId = leaveRequest.LeaveRequestId, Decision = "Rejected", Remarks = "Late" };

            var result = await controller.Decide(dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_NoRecords_ReturnsNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);

            var controller = CreateControllerWithUser(context, 1);

            var result = await controller.GetHistory();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_WithRecords_ReturnsOkList()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);

            var employee = new Employee { EmployeeId = 10, Name = "Emp10", Email = "emp10@test.local", PasswordHash = "x", Role = "Employee", LeaveBalance = 10 };
            var manager = new Employee { EmployeeId = 11, Name = "Mgr10", Email = "mgr10@test.local", PasswordHash = "x", Role = "Manager", LeaveBalance = 20 };
            context.Employees.AddRange(employee, manager);

            var leaveRequest = new LeaveRequest
            {
                LeaveRequestId = 10,
                EmployeeId = employee.EmployeeId,
                Employee = employee,
                LeaveTypeId = 1,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
                Status = "Approved"
            };

            var approval = new Approval
            {
                ApprovalId = 1,
                LeaveRequestId = leaveRequest.LeaveRequestId,
                LeaveRequest = leaveRequest,
                ManagerId = manager.EmployeeId,
                Manager = manager,
                Decision = "Approved",
                Remarks = "ok",
                DecisionDate = DateTime.UtcNow
            };

            context.LeaveRequests.Add(leaveRequest);
            context.Approvals.Add(approval);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, manager.EmployeeId);

            var result = await controller.GetHistory();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
            // Basic check: one record
            var arr = ((System.Collections.IEnumerable)ok.Value).Cast<object>().ToArray();
            Assert.Single(arr);
        }
    }
}
