using System;
using System.Linq;
using System.Threading.Tasks;
using EmployeeLeaveSystem_BackEnd.Data;
using EmployeeLeaveSystem_BackEnd.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmployeeLeaveSystem_Tests.DataTests
{
    public class AppDbContextTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task DbSets_AreAvailable()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateContext(dbName);

            Assert.NotNull(ctx.Employees);
            Assert.NotNull(ctx.LeaveRequests);
            Assert.NotNull(ctx.LeaveTypes);
            Assert.NotNull(ctx.Approvals);

            // ensure database created so model is initialized
            await ctx.Database.EnsureCreatedAsync();
        }

        [Fact]
        public void Constructor_AppliesModelSeeding()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateContext(dbName);

            // Ensure created on this instance (constructor of AppDbContext also calls EnsureCreated)
            ctx.Database.EnsureCreated();

            var leaveTypes = ctx.LeaveTypes.ToList();

            Assert.NotEmpty(leaveTypes);
            Assert.Contains(leaveTypes, lt => lt.Name == "Annual Leave");
            Assert.Contains(leaveTypes, lt => lt.Name == "Sick Leave");
            Assert.Contains(leaveTypes, lt => lt.Name == "Casual Leave");
            Assert.Contains(leaveTypes, lt => lt.Name == "Maternity Leave");
        }

        [Fact]
        public void Model_HasUniqueEmailIndex_OnEmployee()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateContext(dbName);

            var employeeType = ctx.Model.FindEntityType(typeof(Employee));
            Assert.NotNull(employeeType);

            var indexes = employeeType.GetIndexes();
            Assert.Contains(indexes, idx =>
                idx.Properties.Any(p => p.Name == nameof(Employee.Email)) && idx.IsUnique);
        }

        [Fact]
        public void Model_LeaveRequest_HasRequiredRelations()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateContext(dbName);

            var lrType = ctx.Model.FindEntityType(typeof(LeaveRequest));
            Assert.NotNull(lrType);

            // LeaveRequest has FK to Employee
            var empFk = lrType.GetForeignKeys()
                .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Employee));
            Assert.NotNull(empFk);
            Assert.Equal(DeleteBehavior.Cascade, empFk.DeleteBehavior);

            // LeaveRequest has FK to LeaveType
            var ltFk = lrType.GetForeignKeys()
                .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(LeaveType));
            Assert.NotNull(ltFk);
            Assert.Equal(DeleteBehavior.Cascade, ltFk.DeleteBehavior);
        }
    }
}