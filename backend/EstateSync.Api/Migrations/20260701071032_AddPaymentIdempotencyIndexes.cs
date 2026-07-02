using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentIdempotencyIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OwnerSettlements_Users_OwnerId",
                table: "OwnerSettlements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OwnerSettlements",
                table: "OwnerSettlements");

            migrationBuilder.AlterColumn<string>(
                name: "StripePayoutId",
                table: "OwnerSettlements",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "OwnerSettlements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DestinationId",
                table: "OwnerSettlements",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "OwnerSettlements",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "OwnerSettlements",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OwnerSettlementId",
                table: "OwnerSettlements",
                column: "OwnerSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnerSettlements_ArrivalDate",
                table: "OwnerSettlements",
                column: "ArrivalDate");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerSettlements_StripePayoutId",
                table: "OwnerSettlements",
                column: "StripePayoutId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OwnerSettlements_Users_OwnerId",
                table: "OwnerSettlements",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OwnerSettlements_Users_OwnerId",
                table: "OwnerSettlements");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OwnerSettlementId",
                table: "OwnerSettlements");

            migrationBuilder.DropIndex(
                name: "IX_OwnerSettlements_ArrivalDate",
                table: "OwnerSettlements");

            migrationBuilder.DropIndex(
                name: "IX_OwnerSettlements_StripePayoutId",
                table: "OwnerSettlements");

            migrationBuilder.AlterColumn<string>(
                name: "StripePayoutId",
                table: "OwnerSettlements",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "OwnerSettlements",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "DestinationId",
                table: "OwnerSettlements",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "OwnerSettlements",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "OwnerSettlements",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OwnerSettlements",
                table: "OwnerSettlements",
                column: "OwnerSettlementId");

            migrationBuilder.AddForeignKey(
                name: "FK_OwnerSettlements_Users_OwnerId",
                table: "OwnerSettlements",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
