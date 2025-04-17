using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceTrackingSystem.Migrations
{
    /// <inheritdoc />
    public partial class Make_DriverId_Added_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_DriverId1",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "DriverId1",
                table: "AspNetUsers",
                newName: "DriverId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_DriverId1",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_DriverId",
                table: "AspNetUsers",
                column: "DriverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_DriverId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "DriverId",
                table: "AspNetUsers",
                newName: "DriverId1");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_DriverId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_DriverId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_DriverId1",
                table: "AspNetUsers",
                column: "DriverId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
