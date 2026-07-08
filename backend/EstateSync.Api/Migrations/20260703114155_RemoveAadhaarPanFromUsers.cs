using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAadhaarPanFromUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_AadhaarNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PanNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AadhaarNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PanNumber",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_KycVerifications_AadhaarNumber",
                table: "KycVerifications",
                column: "AadhaarNumber",
                unique: true,
                filter: "\"Status\" = 'Approved'");

            migrationBuilder.CreateIndex(
                name: "IX_KycVerifications_PanNumber",
                table: "KycVerifications",
                column: "PanNumber",
                unique: true,
                filter: "\"Status\" = 'Approved'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KycVerifications_AadhaarNumber",
                table: "KycVerifications");

            migrationBuilder.DropIndex(
                name: "IX_KycVerifications_PanNumber",
                table: "KycVerifications");

            migrationBuilder.AddColumn<string>(
                name: "AadhaarNumber",
                table: "Users",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PanNumber",
                table: "Users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "AadhaarNumber", "PanNumber" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Users_AadhaarNumber",
                table: "Users",
                column: "AadhaarNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PanNumber",
                table: "Users",
                column: "PanNumber",
                unique: true);
        }
    }
}
