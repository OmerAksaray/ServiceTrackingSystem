using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceTrackingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexToNormalizedUserName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CityId",
                table: "Locations",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Add index on NormalizedUserName for better login performance
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NormalizedUserName",
                table: "AspNetUsers",
                column: "NormalizedUserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the index when rolling back
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NormalizedUserName",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "CityId",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
