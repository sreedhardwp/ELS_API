using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EmployeeLeaveSystem_BackEnd.Models;
using Xunit;

namespace EmployeeLeaveSystem_Tests.ModelsTests
{
    public class EmployeeTests
    {
        [Fact]
        public void PublicInstanceMethods_NoneExistExceptPropertyAccessors()
        {
            var methods = typeof(Employee).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .ToList();

            Assert.Empty(methods);
        }

        [Fact]
        public void Properties_HaveExpectedAttributesAndDefaults()
        {
            var type = typeof(Employee);

            var idProp = type.GetProperty(nameof(Employee.EmployeeId));
            Assert.NotNull(idProp);
            Assert.True(Attribute.IsDefined(idProp!, typeof(KeyAttribute)));

            var nameProp = type.GetProperty(nameof(Employee.Name));
            Assert.NotNull(nameProp);
            Assert.True(Attribute.IsDefined(nameProp!, typeof(RequiredAttribute)));
            var nameMax = nameProp!.GetCustomAttribute<MaxLengthAttribute>();
            Assert.NotNull(nameMax);
            Assert.Equal(100, nameMax.Length);

            var emailProp = type.GetProperty(nameof(Employee.Email));
            Assert.NotNull(emailProp);
            Assert.True(Attribute.IsDefined(emailProp!, typeof(RequiredAttribute)));
            var emailMax = emailProp!.GetCustomAttribute<MaxLengthAttribute>();
            Assert.NotNull(emailMax);
            Assert.Equal(150, emailMax.Length);
            Assert.True(Attribute.IsDefined(emailProp!, typeof(EmailAddressAttribute)));

            var pwdProp = type.GetProperty(nameof(Employee.PasswordHash));
            Assert.NotNull(pwdProp);
            Assert.True(Attribute.IsDefined(pwdProp!, typeof(RequiredAttribute)));

            var deptProp = type.GetProperty(nameof(Employee.Department));
            Assert.NotNull(deptProp);
            var deptMax = deptProp!.GetCustomAttribute<MaxLengthAttribute>();
            Assert.NotNull(deptMax);
            Assert.Equal(100, deptMax.Length);

            var roleProp = type.GetProperty(nameof(Employee.Role));
            Assert.NotNull(roleProp);
            Assert.True(Attribute.IsDefined(roleProp!, typeof(RequiredAttribute)));

            var balanceProp = type.GetProperty(nameof(Employee.LeaveBalance));
            Assert.NotNull(balanceProp);

            var lrNav = type.GetProperty(nameof(Employee.LeaveRequests));
            Assert.NotNull(lrNav);
            Assert.Contains("ICollection", lrNav!.PropertyType.Name);

            var approvalsNav = type.GetProperty(nameof(Employee.Approvals));
            Assert.NotNull(approvalsNav);
            Assert.Contains("ICollection", approvalsNav!.PropertyType.Name);
        }

        [Fact]
        public void DefaultValues_AreSetOnInstantiation()
        {
            var inst = Activator.CreateInstance<Employee>();

            Assert.Equal(string.Empty, inst.Name);
            Assert.Equal(string.Empty, inst.Email);
            Assert.Equal(string.Empty, inst.PasswordHash);
            Assert.Equal(string.Empty, inst.Department);
            Assert.Equal("Employee", inst.Role);
            Assert.Equal(20, inst.LeaveBalance);
            Assert.NotNull(inst.LeaveRequests);
            Assert.NotNull(inst.Approvals);
        }
    }
}