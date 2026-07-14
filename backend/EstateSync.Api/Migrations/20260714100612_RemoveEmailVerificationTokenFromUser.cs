using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmailVerificationTokenFromUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_EmailVerificationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenExpiresAt",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "Users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenExpiresAt",
                table: "Users",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "EmailVerificationToken", "EmailVerificationTokenExpiresAt" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailVerificationToken",
                table: "Users",
                column: "EmailVerificationToken");
        }
    }
}
