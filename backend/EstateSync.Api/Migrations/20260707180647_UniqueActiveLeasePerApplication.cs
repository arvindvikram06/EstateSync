using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class UniqueActiveLeasePerApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leases_ApplicationId",
                table: "Leases");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_ApplicationId",
                table: "Leases",
                column: "ApplicationId",
                unique: true,
                filter: "\"Status\" NOT IN ('Expired', 'Terminated', 'Voided')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leases_ApplicationId",
                table: "Leases");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_ApplicationId",
                table: "Leases",
                column: "ApplicationId");
        }
    }
}
