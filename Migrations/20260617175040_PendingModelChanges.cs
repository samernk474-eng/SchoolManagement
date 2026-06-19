using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_SchoolId_Role",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SchoolId_Role",
                table: "Employees",
                columns: new[] { "SchoolId", "Role" },
                unique: true,
                filter: "[Role] IN (N'Principal', N'Secretary', N'Librarian', N'ActivitySupervisor') AND [IsDismissed] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_SchoolId_Role",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SchoolId_Role",
                table: "Employees",
                columns: new[] { "SchoolId", "Role" },
                unique: true,
                filter: "[Role] IN (N'Manager', N'Secretary', N'Librarian', N'ActivitySupervisor') AND [IsDismissed] = 0");
        }
    }
}
