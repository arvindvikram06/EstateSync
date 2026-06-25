using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class LocationUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "State",
                table: "Locations",
                newName: "StateName");

            migrationBuilder.RenameIndex(
                name: "IX_Locations_State",
                table: "Locations",
                newName: "IX_Locations_StateName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StateName",
                table: "Locations",
                newName: "State");

            migrationBuilder.RenameIndex(
                name: "IX_Locations_StateName",
                table: "Locations",
                newName: "IX_Locations_State");
        }
    }
}
