using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFinancialProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantFinancialDocuments");

            migrationBuilder.DropTable(
                name: "TenantFinancialProfiles");

            migrationBuilder.DropColumn(
                name: "IsFinanciallyVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VerifiedMonthlyIncome",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RequiresVerifiedIncome",
                table: "Properties");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFinanciallyVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "VerifiedMonthlyIncome",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresVerifiedIncome",
                table: "Properties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TenantFinancialProfiles",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeclaredMonthlyIncome = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EmployerName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    EmploymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantFinancialProfileId", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_TenantFinancialProfiles_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantFinancialProfiles_Users_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantFinancialDocuments",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantFinancialDocument", x => new { x.ProfileId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_TenantFinancialDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantFinancialDocuments_TenantFinancialProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "TenantFinancialProfiles",
                        principalColumn: "ProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "VerifiedMonthlyIncome",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_TenantFinancialDocuments_DocumentId",
                table: "TenantFinancialDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantFinancialProfiles_ReviewedById",
                table: "TenantFinancialProfiles",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_TenantFinancialProfiles_TenantId",
                table: "TenantFinancialProfiles",
                column: "TenantId");
        }
    }
}
