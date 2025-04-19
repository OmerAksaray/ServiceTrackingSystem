using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceTrackingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AdminAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressText",
                table: "EmployeeAddresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "AddressText",
                table: "EmployeeAddresses");
        }
    }
}
