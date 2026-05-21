using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using EmployeeLeaveSystem_BackEnd.Models;
using Xunit;

namespace EmployeeLeaveSystem_Tests.ModelsTests
{
    public class LeaveTypeTests
    {
        [Fact]
        public void PublicInstanceMethods_NoneExistExceptPropertyAccessors()
        {
            var methods = typeof(LeaveType).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .ToList();

            Assert.Empty(methods);
        }

        [Fact]
        public void Properties_HaveExpectedAttributesAndDefaults()
        {
            var type = typeof(LeaveType);

            var idProp = type.GetProperty(nameof(LeaveType.LeaveTypeId));
            Assert.NotNull(idProp);
            Assert.True(Attribute.IsDefined(idProp!, typeof(KeyAttribute)));

            var nameProp = type.GetProperty(nameof(LeaveType.Name));
            Assert.NotNull(nameProp);
            Assert.True(Attribute.IsDefined(nameProp!, typeof(RequiredAttribute)));
            var nameMax = nameProp!.GetCustomAttribute<MaxLengthAttribute>();
            Assert.NotNull(nameMax);
            Assert.Equal(50, nameMax.Length);

            var maxDaysProp = type.GetProperty(nameof(LeaveType.MaxDaysAllowed));
            Assert.NotNull(maxDaysProp);

            var lrNav = type.GetProperty(nameof(LeaveType.LeaveRequests));
            Assert.NotNull(lrNav);
            Assert.Contains("ICollection", lrNav!.PropertyType.Name);
        }

        [Fact]
        public void DefaultValues_AreSetOnInstantiation()
        {
            var inst = Activator.CreateInstance<LeaveType>();

            Assert.Equal(string.Empty, inst.Name);
            Assert.Equal(0, inst.MaxDaysAllowed);
            Assert.NotNull(inst.LeaveRequests);
        }
    }
}