using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTrackingAndCustomerConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StripeCustomerProfiles_Users_TenantId",
                table: "StripeCustomerProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "StripeCustomerId",
                table: "StripeCustomerProfiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ActiveStripeSessionId",
                table: "LeaseBills",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StripeCustomerProfiles_StripeCustomerId",
                table: "StripeCustomerProfiles",
                column: "StripeCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_StripeCustomerProfiles_Users_TenantId",
                table: "StripeCustomerProfiles",
                column: "TenantId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StripeCustomerProfiles_Users_TenantId",
                table: "StripeCustomerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_StripeCustomerProfiles_StripeCustomerId",
                table: "StripeCustomerProfiles");

            migrationBuilder.DropColumn(
                name: "ActiveStripeSessionId",
                table: "LeaseBills");

            migrationBuilder.AlterColumn<string>(
                name: "StripeCustomerId",
                table: "StripeCustomerProfiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddForeignKey(
                name: "FK_StripeCustomerProfiles_Users_TenantId",
                table: "StripeCustomerProfiles",
                column: "TenantId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
