using System;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using EmployeeLeaveSystem_BackEnd.Controllers;
using EmployeeLeaveSystem_BackEnd.DTOs;
using EmployeeLeaveSystem_BackEnd.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EmployeeLeaveSystem_Tests.ControllerTests
{
    public class LeaveRequestControllerTests
    {
        private class FakeLeaveService : ILeaveRequestService
        {
            private readonly Func<LeaveRequestDto.CreateLeaveRequestDto, int, Task<bool>> _apply;
            private readonly Func<int, Task<List<LeaveRequestDto.LeaveRequestResponseDto>>> _my;
            private readonly Func<Task<List<LeaveRequestDto.LeaveRequestResponseDto>>> _pending;
            private readonly Func<int, int, Task<bool>> _cancel;

            public FakeLeaveService(
                Func<LeaveRequestDto.CreateLeaveRequestDto, int, Task<bool>> apply,
                Func<int, Task<List<LeaveRequestDto.LeaveRequestResponseDto>>> my,
                Func<Task<List<LeaveRequestDto.LeaveRequestResponseDto>>> pending,
                Func<int, int, Task<bool>> cancel)
            {
                _apply = apply;
                _my = my;
                _pending = pending;
                _cancel = cancel;
            }

            public Task<bool> ApplyLeaveAsync(LeaveRequestDto.CreateLeaveRequestDto dto, int employeeId) => _apply(dto, employeeId);

            public Task<List<LeaveRequestDto.LeaveRequestResponseDto>> GetMyLeavesAsync(int employeeId) => _my(employeeId);

            public Task<List<LeaveRequestDto.LeaveRequestResponseDto>> GetPendingLeavesAsync() => _pending();

            public Task<bool> CancelLeaveAsync(int leaveRequestId, int employeeId) => _cancel(leaveRequestId, employeeId);
        }

        private static LeaveRequestController CreateControllerWithUser(ILeaveRequestService svc, int? userId = null)
        {
            var controller = new LeaveRequestController(svc);
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

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return controller;
        }

        [Fact]
        public async Task GetMyLeaves_Unauthorized_WhenNoClaim()
        {
            var svc = new FakeLeaveService((d, id) => Task.FromResult(true), id => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()),
                () => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()), (i, id) => Task.FromResult(true));

            var controller = CreateControllerWithUser(svc, null);

            var result = await controller.GetMyLeaves();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetMyLeaves_NotFound_WhenNoRecords()
        {
            var svc = new FakeLeaveService((d, id) => Task.FromResult(true), id => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()),
                () => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()), (i, id) => Task.FromResult(true));

            var controller = CreateControllerWithUser(svc, 1);

            var result = await controller.GetMyLeaves();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetMyLeaves_Success_ReturnsOk()
        {
            var list = new List<LeaveRequestDto.LeaveRequestResponseDto>
            {
                new LeaveRequestDto.LeaveRequestResponseDto { LeaveRequestId = 1, EmployeeName = "E", LeaveTypeName = "Annual", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow, Status = "Pending" }
            };

            var svc = new FakeLeaveService((d, id) => Task.FromResult(true), id => Task.FromResult(list),
                () => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()), (i, id) => Task.FromResult(true));

            var controller = CreateControllerWithUser(svc, 2);

            var result = await controller.GetMyLeaves();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(list, ok.Value);
        }

        [Fact]
        public async Task GetPendingLeaves_ReturnsOk_WithEmptyAndWithItems()
        {
            var svcEmpty = new FakeLeaveService((d, id) => Task.FromResult(true), id => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()),
                () => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()), (i, id) => Task.FromResult(true));

            var controllerEmpty = CreateControllerWithUser(svcEmpty, 1);
            var resEmpty = await controllerEmpty.GetPendingLeaves();
            var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
            Assert.IsAssignableFrom<IEnumerable<LeaveRequestDto.LeaveRequestResponseDto>>(okEmpty.Value);

            var items = new List<LeaveRequestDto.LeaveRequestResponseDto>
            {
                new LeaveRequestDto.LeaveRequestResponseDto { LeaveRequestId = 5, EmployeeName = "E2", LeaveTypeName = "Sick", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow, Status = "Pending" }
            };

            var svcItems = new FakeLeaveService((d, id) => Task.FromResult(true), id => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()),
                () => Task.FromResult(items), (i, id) => Task.FromResult(true));

            var controllerItems = CreateControllerWithUser(svcItems, 1);
            var resItems = await controllerItems.GetPendingLeaves();
            var okItems = Assert.IsType<OkObjectResult>(resItems);
            Assert.Equal(items, okItems.Value);
        }

        [Fact]
        public async Task ApplyLeave_ModelStateInvalid_ReturnsBadRequest()
        {
            var svc = new FakeLeaveService((d, id) => Task.FromResult(true), id => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()),
                () => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()), (i, id) => Task.FromResult(true));

            var controller = CreateControllerWithUser(svc, 1);
            controller.ModelState.AddModelError("StartDate", "Required");

            var dto = new LeaveRequestDto.CreateLeaveRequestDto();
            var result = await controller.ApplyLeave(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ApplyLeave_Unauthorized_WhenNoClaim()
        {
            var svc = new FakeLeaveService((d, id) => Task.FromResult(true), id => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()),
                () => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()), (i, id) => Task.FromResult(true));

            var controller = CreateControllerWithUser(svc, null);

            var dto = new LeaveRequestDto.CreateLeaveRequestDto();
            var result = await controller.ApplyLeave(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task ApplyLeave_Failure_ReturnsBadRequest()
        {
            var svc = new FakeLeaveService((d, id) => Task.FromResult(false), id => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()),
                () => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()), (i, id) => Task.FromResult(true));

            var controller = CreateControllerWithUser(svc, 3);

            var dto = new LeaveRequestDto.CreateLeaveRequestDto { LeaveTypeId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow };
            var result = await controller.ApplyLeave(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ApplyLeave_Success_ReturnsOk()
        {
            var svc = new FakeLeaveService((d, id) => Task.FromResult(true), id => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()),
                () => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()), (i, id) => Task.FromResult(true));

            var controller = CreateControllerWithUser(svc, 4);

            var dto = new LeaveRequestDto.CreateLeaveRequestDto { LeaveTypeId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow };
            var result = await controller.ApplyLeave(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Leave applied successfully", ok.Value?.ToString());
        }

        [Fact]
        public async Task CancelLeave_Unauthorized_WhenNoClaim()
        {
            var svc = new FakeLeaveService((d, id) => Task.FromResult(true), id => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()),
                () => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()), (i, id) => Task.FromResult(true));

            var controller = CreateControllerWithUser(svc, null);

            var result = await controller.CancelLeave(1);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task CancelLeave_Failure_ReturnsBadRequest()
        {
            var svc = new FakeLeaveService((d, id) => Task.FromResult(true), id => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()),
                () => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()), (i, id) => Task.FromResult(false));

            var controller = CreateControllerWithUser(svc, 5);

            var result = await controller.CancelLeave(2);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CancelLeave_Success_ReturnsOk()
        {
            var svc = new FakeLeaveService((d, id) => Task.FromResult(true), id => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()),
                () => Task.FromResult(new List<LeaveRequestDto.LeaveRequestResponseDto>()), (i, id) => Task.FromResult(true));

            var controller = CreateControllerWithUser(svc, 6);

            var result = await controller.CancelLeave(3);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Leave cancelled successfully", ok.Value?.ToString());
        }
    }
}
