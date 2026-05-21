using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EmployeeLeaveSystem_BackEnd.Models;
using Xunit;

namespace EmployeeLeaveSystem_Tests.ModelsTests
{
    public class LeaveRequestTests
    {
        [Fact]
        public void PublicInstanceMethods_NoneExistExceptPropertyAccessors()
        {
            var methods = typeof(LeaveRequest).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .ToList();

            Assert.Empty(methods);
        }

        [Fact]
        public void Properties_HaveExpectedAttributesAndDefaults()
        {
            var type = typeof(LeaveRequest);

            var idProp = type.GetProperty(nameof(LeaveRequest.LeaveRequestId));
            Assert.NotNull(idProp);
            Assert.True(Attribute.IsDefined(idProp!, typeof(KeyAttribute)));

            var empIdProp = type.GetProperty(nameof(LeaveRequest.EmployeeId));
            Assert.NotNull(empIdProp);
            var fkEmp = empIdProp!.GetCustomAttribute<ForeignKeyAttribute>();
            Assert.NotNull(fkEmp);
            Assert.Equal("Employee", fkEmp.Name);

            var ltIdProp = type.GetProperty(nameof(LeaveRequest.LeaveTypeId));
            Assert.NotNull(ltIdProp);
            var fkLt = ltIdProp!.GetCustomAttribute<ForeignKeyAttribute>();
            Assert.NotNull(fkLt);
            Assert.Equal("LeaveType", fkLt.Name);

            var startProp = type.GetProperty(nameof(LeaveRequest.StartDate));
            Assert.NotNull(startProp);
            Assert.True(Attribute.IsDefined(startProp!, typeof(RequiredAttribute)));
            Assert.Equal(typeof(DateTime), startProp!.PropertyType);

            var endProp = type.GetProperty(nameof(LeaveRequest.EndDate));
            Assert.NotNull(endProp);
            Assert.True(Attribute.IsDefined(endProp!, typeof(RequiredAttribute)));
            Assert.Equal(typeof(DateTime), endProp!.PropertyType);

            var reasonProp = type.GetProperty(nameof(LeaveRequest.Reason));
            Assert.NotNull(reasonProp);
            var maxLen = reasonProp!.GetCustomAttribute<MaxLengthAttribute>();
            Assert.NotNull(maxLen);
            Assert.Equal(500, maxLen.Length);

            var statusProp = type.GetProperty(nameof(LeaveRequest.Status));
            Assert.NotNull(statusProp);

            var requestedOnProp = type.GetProperty(nameof(LeaveRequest.RequestedOn));
            Assert.NotNull(requestedOnProp);

            var approvalProp = type.GetProperty(nameof(LeaveRequest.Approval));
            Assert.NotNull(approvalProp);
        }

        [Fact]
        public void DefaultValues_AreSetOnInstantiation()
        {
            var inst = Activator.CreateInstance<LeaveRequest>();

            Assert.Equal(string.Empty, inst.Reason);
            Assert.Equal("Pending", inst.Status);

            var deltaSeconds = Math.Abs((DateTime.UtcNow - inst.RequestedOn).TotalSeconds);
            Assert.True(deltaSeconds < 5, $"RequestedOn should be close to UtcNow but differed by {deltaSeconds} seconds.");

            Assert.Null(inst.Approval);
        }
    }
}