using System;
using System.Collections.Generic;
using System.Linq;
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
    // ✅ Manual fake — no Moq needed
    public class FakeApprovalService : IApprovalService
    {
        public IActionResult DecideResult { get; set; }
        public IActionResult HistoryResult { get; set; }
        public bool ThrowOnDecide { get; set; } = false;
        public bool ThrowOnHistory { get; set; } = false;

        public Task<IActionResult> DecideAsync(ApprovalDto.ApprovalRequestDto dto, ClaimsPrincipal user)
        {
            if (ThrowOnDecide)
                throw new Exception("Simulated service failure");

            return Task.FromResult(DecideResult);
        }

        public Task<IActionResult> GetHistoryAsync()
        {
            if (ThrowOnHistory)
                throw new Exception("Simulated service failure");

            return Task.FromResult(HistoryResult);
        }
    }

    public class ApprovalControllerTests
    {
        // ✅ Creates controller with fake service and optional user claim
        private static ApprovalController CreateController(
            FakeApprovalService fakeService,
            int? userId = null)
        {
            var controller = new ApprovalController(fakeService);

            var httpContext = new DefaultHttpContext();
            if (userId.HasValue)
            {
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
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

        // ─── Decide Tests ────────────────────────────────────────────────

        [Fact]
        public async Task Decide_Approve_ReturnsOk()
        {
            var fakeService = new FakeApprovalService
            {
                DecideResult = new OkObjectResult("Approved successfully")
            };

            var dto = new ApprovalDto.ApprovalRequestDto
            {
                LeaveRequestId = 1,
                Decision = "Approved",
                Remarks = "OK"
            };

            var controller = CreateController(fakeService, userId: 2);
            var result = await controller.Decide(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Approved", ok.Value?.ToString());
        }

        [Fact]
        public async Task Decide_Should_Fail_When_Expecting_Ok_But_BadRequest_Returned()
        {
            var fakeService = new FakeApprovalService
            {
                DecideResult = new BadRequestObjectResult("Approval failed")
            };

            var dto = new ApprovalDto.ApprovalRequestDto
            {
                LeaveRequestId = 1,
                Decision = "Approved",
                Remarks = "OK"
            };

            var controller = CreateController(fakeService, userId: 2);
            var result = await controller.Decide(dto);

            
            var ok = Assert.IsType<OkObjectResult>(result);
        }



        [Fact]
        public async Task Decide_InsufficientBalance_ReturnsBadRequest()
        {
            var fakeService = new FakeApprovalService
            {
                DecideResult = new BadRequestObjectResult("Insufficient leave balance")
            };

            var dto = new ApprovalDto.ApprovalRequestDto
            {
                LeaveRequestId = 2,
                Decision = "Approved",
                Remarks = "OK"
            };

            var controller = CreateController(fakeService, userId: 2);
            var result = await controller.Decide(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Insufficient leave balance", bad.Value?.ToString());
        }

        [Fact]
        public async Task Decide_InvalidDecision_ReturnsBadRequest()
        {
            var fakeService = new FakeApprovalService
            {
                DecideResult = new BadRequestObjectResult("Decision must be 'Approved' or 'Rejected'")
            };

            var dto = new ApprovalDto.ApprovalRequestDto
            {
                LeaveRequestId = 3,
                Decision = "Maybe",
                Remarks = "?"
            };

            var controller = CreateController(fakeService, userId: 4);
            var result = await controller.Decide(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Decision must be", bad.Value?.ToString());
        }

        [Fact]
        public async Task Decide_NoUserClaim_ReturnsUnauthorized()
        {
            var fakeService = new FakeApprovalService
            {
                DecideResult = new UnauthorizedObjectResult("Unauthorized")
            };

            var dto = new ApprovalDto.ApprovalRequestDto
            {
                LeaveRequestId = 5,
                Decision = "Approved",
                Remarks = "OK"
            };

            var controller = CreateController(fakeService, userId: null); // no user
            var result = await controller.Decide(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Decide_AlreadyProcessed_ReturnsNotFound()
        {
            var fakeService = new FakeApprovalService
            {
                DecideResult = new NotFoundObjectResult("Already processed")
            };

            var dto = new ApprovalDto.ApprovalRequestDto
            {
                LeaveRequestId = 6,
                Decision = "Rejected",
                Remarks = "Late"
            };

            var controller = CreateController(fakeService, userId: 7);
            var result = await controller.Decide(dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Decide_ServiceThrowsException_Returns500()
        {
            var fakeService = new FakeApprovalService
            {
                ThrowOnDecide = true  // ✅ triggers exception inside fake
            };

            var dto = new ApprovalDto.ApprovalRequestDto
            {
                LeaveRequestId = 99,
                Decision = "Approved",
                Remarks = "OK"
            };

            var controller = CreateController(fakeService, userId: 2);
            var result = await controller.Decide(dto);

            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        // ─── GetHistory Tests ────────────────────────────────────────────

        [Fact]
        public async Task GetHistory_NoRecords_ReturnsNotFound()
        {
            var fakeService = new FakeApprovalService
            {
                HistoryResult = new NotFoundObjectResult("No approval history found")
            };

            var controller = CreateController(fakeService, userId: 1);
            var result = await controller.GetHistory();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_WithRecords_ReturnsOkList()
        {
            var fakeHistory = new List<object>
            {
                new { ApprovalId = 1, Decision = "Approved", ManagerId = 11, LeaveRequestId = 10 }
            };

            var fakeService = new FakeApprovalService
            {
                HistoryResult = new OkObjectResult(fakeHistory)
            };

            var controller = CreateController(fakeService, userId: 11);
            var result = await controller.GetHistory();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
            Assert.Single(list.Cast<object>());
        }

        [Fact]
        public async Task GetHistory_ServiceThrowsException_Returns500()
        {
            var fakeService = new FakeApprovalService
            {
                ThrowOnHistory = true  // ✅ triggers exception inside fake
            };

            var controller = CreateController(fakeService, userId: 1);
            var result = await controller.GetHistory();

            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }
    }
}