using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeConnectedAccountStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChargesEnabled",
                table: "StripeConnectedAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DetailsSubmitted",
                table: "StripeConnectedAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "StripeConnectedAccounts",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PayoutsEnabled",
                table: "StripeConnectedAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargesEnabled",
                table: "StripeConnectedAccounts");

            migrationBuilder.DropColumn(
                name: "DetailsSubmitted",
                table: "StripeConnectedAccounts");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "StripeConnectedAccounts");

            migrationBuilder.DropColumn(
                name: "PayoutsEnabled",
                table: "StripeConnectedAccounts");
        }
    }
}
