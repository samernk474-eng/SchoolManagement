using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeEmployeePhoneUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_Phone",
                table: "Employees");

            migrationBuilder.Sql("UPDATE Employees SET Phone = NULL WHERE Phone = ''");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Employees",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Phone",
                table: "Employees",
                column: "Phone",
                unique: true,
                filter: "[Phone] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_Phone",
                table: "Employees");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Employees",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Phone",
                table: "Employees",
                column: "Phone",
                unique: true);
        }
    }
}
