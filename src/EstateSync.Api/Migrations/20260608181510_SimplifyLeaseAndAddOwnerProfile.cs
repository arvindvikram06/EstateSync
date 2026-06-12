using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyLeaseAndAddOwnerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leases_AgreementTemplates_TemplateId",
                table: "Leases");

            migrationBuilder.DropForeignKey(
                name: "FK_Leases_AgreementTypes_AgreementTypeId",
                table: "Leases");

            migrationBuilder.DropForeignKey(
                name: "FK_Properties_AgreementTypes_AgreementTypeId",
                table: "Properties");

            migrationBuilder.DropTable(
                name: "AgreementAcknowledgements");

            migrationBuilder.DropTable(
                name: "AgreementTemplates");

            migrationBuilder.DropTable(
                name: "AgreementTypes");

            migrationBuilder.DropIndex(
                name: "IX_Properties_AgreementTypeId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Leases_AgreementTypeId",
                table: "Leases");

            migrationBuilder.DropIndex(
                name: "IX_Leases_TemplateId",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "AgreementTypeId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "AgreementData",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "AgreementTypeId",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "FinalAgreementHtml",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "Leases");

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
                name: "BankIFSCCode",
                table: "OwnerVerificationRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "OwnerVerificationRequests",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AgreementDocumentUrl",
                table: "Leases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgreementText",
                table: "Leases",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerSignature",
                table: "Leases",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OwnerSignedAt",
                table: "Leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedAgreementUrl",
                table: "Leases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantSignature",
                table: "Leases",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TenantSignedAt",
                table: "Leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnsignedAgreementUrl",
                table: "Leases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OwnerProfiles",
                columns: table => new
                {
                    OwnerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankAccountNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BankIFSCCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BankAccountHolderName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    BankName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerProfileId", x => x.OwnerProfileId);
                    table.ForeignKey(
                        name: "FK_OwnerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerProfiles_UserId",
                table: "OwnerProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "BankAccountHolderName",
                table: "OwnerVerificationRequests");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "OwnerVerificationRequests");

            migrationBuilder.DropColumn(
                name: "BankIFSCCode",
                table: "OwnerVerificationRequests");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "OwnerVerificationRequests");

            migrationBuilder.DropColumn(
                name: "AgreementText",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "OwnerSignature",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "OwnerSignedAt",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "SignedAgreementUrl",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "TenantSignature",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "TenantSignedAt",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "UnsignedAgreementUrl",
                table: "Leases");

            migrationBuilder.AddColumn<Guid>(
                name: "AgreementTypeId",
                table: "Properties",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AgreementDocumentUrl",
                table: "Leases",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgreementData",
                table: "Leases",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AgreementTypeId",
                table: "Leases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "FinalAgreementHtml",
                table: "Leases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId",
                table: "Leases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AgreementAcknowledgements",
                columns: table => new
                {
                    AcknowledgementId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Acknowledge = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgreementAcknowledgementId", x => x.AcknowledgementId);
                    table.ForeignKey(
                        name: "FK_AgreementAcknowledgements_Leases_LeaseId",
                        column: x => x.LeaseId,
                        principalTable: "Leases",
                        principalColumn: "LeaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgreementAcknowledgements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgreementTypes",
                columns: table => new
                {
                    AgreementTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowsRentIncrease = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DefaultPenaltyPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequiresNoticePeriod = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgreementTypeId", x => x.AgreementTypeId);
                });

            migrationBuilder.CreateTable(
                name: "AgreementTemplates",
                columns: table => new
                {
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PlaceholderSchema = table.Column<string>(type: "jsonb", nullable: true),
                    TemplateName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgreementTemplateId", x => x.TemplateId);
                    table.ForeignKey(
                        name: "FK_AgreementTemplates_AgreementTypes_AgreementTypeId",
                        column: x => x.AgreementTypeId,
                        principalTable: "AgreementTypes",
                        principalColumn: "AgreementTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_AgreementTypeId",
                table: "Properties",
                column: "AgreementTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_AgreementTypeId",
                table: "Leases",
                column: "AgreementTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_TemplateId",
                table: "Leases",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AgreementAcknowledgements_LeaseId_UserId",
                table: "AgreementAcknowledgements",
                columns: new[] { "LeaseId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgreementAcknowledgements_UserId",
                table: "AgreementAcknowledgements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgreementTemplates_AgreementTypeId",
                table: "AgreementTemplates",
                column: "AgreementTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AgreementTypes_Name",
                table: "AgreementTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Leases_AgreementTemplates_TemplateId",
                table: "Leases",
                column: "TemplateId",
                principalTable: "AgreementTemplates",
                principalColumn: "TemplateId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Leases_AgreementTypes_AgreementTypeId",
                table: "Leases",
                column: "AgreementTypeId",
                principalTable: "AgreementTypes",
                principalColumn: "AgreementTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_AgreementTypes_AgreementTypeId",
                table: "Properties",
                column: "AgreementTypeId",
                principalTable: "AgreementTypes",
                principalColumn: "AgreementTypeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
