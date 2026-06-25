using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRentalApplicationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoveInDate",
                table: "RentalApplications");

            migrationBuilder.DropColumn(
                name: "NegotiatedPrice",
                table: "RentalApplications");

            migrationBuilder.RenameColumn(
                name: "MoveOutDate",
                table: "RentalApplications",
                newName: "ExpectedMoveInDate");

            migrationBuilder.AddColumn<int>(
                name: "DurationMonths",
                table: "RentalApplications",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationMonths",
                table: "RentalApplications");

            migrationBuilder.RenameColumn(
                name: "ExpectedMoveInDate",
                table: "RentalApplications",
                newName: "MoveOutDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "MoveInDate",
                table: "RentalApplications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "NegotiatedPrice",
                table: "RentalApplications",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
