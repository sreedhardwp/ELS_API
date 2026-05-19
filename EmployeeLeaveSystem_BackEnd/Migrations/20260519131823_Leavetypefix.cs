using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeLeaveSystem_BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class Leavetypefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "LeaveTypeId",
                keyValue: 1,
                column: "Name",
                value: "Annual Leave");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "LeaveTypeId",
                keyValue: 2,
                column: "Name",
                value: "Sick Leave");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "LeaveTypeId",
                keyValue: 3,
                column: "Name",
                value: "Casual Leave");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "LeaveTypeId",
                keyValue: 4,
                column: "Name",
                value: "Maternity Leave");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "LeaveTypeId",
                keyValue: 1,
                column: "Name",
                value: "Annual");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "LeaveTypeId",
                keyValue: 2,
                column: "Name",
                value: "Sick");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "LeaveTypeId",
                keyValue: 3,
                column: "Name",
                value: "Casual");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "LeaveTypeId",
                keyValue: 4,
                column: "Name",
                value: "Maternity");
        }
    }
}
