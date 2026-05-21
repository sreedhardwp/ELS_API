using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EmployeeLeaveSystem_BackEnd.Models;
using Xunit;

namespace EmployeeLeaveSystem_Tests.ModelsTests
{
    public class ApprovalTests
    {
        [Fact]
        public void PublicInstanceMethods_NoneExistExceptPropertyAccessors()
        {
            var methods = typeof(Approval).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName) // exclude property getters/setters and other compiler-generated members
                .ToList();

            Assert.Empty(methods);
        }

        [Fact]
        public void Properties_HaveExpectedAttributesAndTypes()
        {
            var type = typeof(Approval);

            var approvalIdProp = type.GetProperty(nameof(Approval.ApprovalId));
            Assert.NotNull(approvalIdProp);
            Assert.True(Attribute.IsDefined(approvalIdProp!, typeof(KeyAttribute)));

            var leaveRequestIdProp = type.GetProperty(nameof(Approval.LeaveRequestId));
            Assert.NotNull(leaveRequestIdProp);
            var fkLeaveAttr = leaveRequestIdProp!.GetCustomAttribute<ForeignKeyAttribute>();
            Assert.NotNull(fkLeaveAttr);
            Assert.Equal("LeaveRequest", fkLeaveAttr.Name);

            var managerIdProp = type.GetProperty(nameof(Approval.ManagerId));
            Assert.NotNull(managerIdProp);
            var fkManagerAttr = managerIdProp!.GetCustomAttribute<ForeignKeyAttribute>();
            Assert.NotNull(fkManagerAttr);
            Assert.Equal("Manager", fkManagerAttr.Name);

            var decisionProp = type.GetProperty(nameof(Approval.Decision));
            Assert.NotNull(decisionProp);
            Assert.True(Attribute.IsDefined(decisionProp!, typeof(RequiredAttribute)));

            var remarksProp = type.GetProperty(nameof(Approval.Remarks));
            Assert.NotNull(remarksProp);
            var maxLenAttr = remarksProp!.GetCustomAttribute<MaxLengthAttribute>();
            Assert.NotNull(maxLenAttr);
            Assert.Equal(500, maxLenAttr.Length);

            var decisionDateProp = type.GetProperty(nameof(Approval.DecisionDate));
            Assert.NotNull(decisionDateProp);

            var leaveRequestNav = type.GetProperty(nameof(Approval.LeaveRequest));
            Assert.NotNull(leaveRequestNav);
            Assert.Equal(typeof(LeaveRequest), leaveRequestNav!.PropertyType);

            var managerNav = type.GetProperty(nameof(Approval.Manager));
            Assert.NotNull(managerNav);
            Assert.Equal(typeof(Employee), managerNav!.PropertyType);
        }

        [Fact]
        public void DefaultValues_AreSetOnInstantiation()
        {
            var inst = Activator.CreateInstance<Approval>();

            // Decision initialized to empty string in class
            Assert.Equal(string.Empty, inst.Decision);

            // Remarks is nullable and should be null by default
            Assert.Null(inst.Remarks);

            // DecisionDate set to UtcNow at instantiation; allow small delta
            var deltaSeconds = Math.Abs((DateTime.UtcNow - inst.DecisionDate).TotalSeconds);
            Assert.True(deltaSeconds < 5, $"DecisionDate should be close to UtcNow but differed by {deltaSeconds} seconds.");
        }
    }
}