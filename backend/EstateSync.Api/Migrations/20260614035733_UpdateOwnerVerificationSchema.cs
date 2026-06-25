using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOwnerVerificationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountHolderName",
                table: "OwnerVerificationRequests");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "OwnerVerificationRequests");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "OwnerVerificationRequests");

            migrationBuilder.DropColumn(
                name: "BankAccountHolderName",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "OwnerProfiles");

            migrationBuilder.RenameColumn(
                name: "BankIFSCCode",
                table: "OwnerVerificationRequests",
                newName: "PanOrTaxId");

            migrationBuilder.RenameColumn(
                name: "BankIFSCCode",
                table: "OwnerProfiles",
                newName: "PanOrTaxId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PanOrTaxId",
                table: "OwnerVerificationRequests",
                newName: "BankIFSCCode");

            migrationBuilder.RenameColumn(
                name: "PanOrTaxId",
                table: "OwnerProfiles",
                newName: "BankIFSCCode");

            migrationBuilder.AddColumn<string>(
                name: "BankAccountHolderName",
                table: "OwnerVerificationRequests",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "OwnerVerificationRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "OwnerVerificationRequests",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountHolderName",
                table: "OwnerProfiles",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "OwnerProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "OwnerProfiles",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");
        }
    }
}
