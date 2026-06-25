using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class RedesignVerificationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityVerificationDocuments");

            migrationBuilder.DropTable(
                name: "OwnerProfiles");

            migrationBuilder.DropTable(
                name: "TenantVerificationDocuments");

            migrationBuilder.DropTable(
                name: "IdentityVerificationRequests");

            migrationBuilder.DropTable(
                name: "TenantVerificationRequests");

            migrationBuilder.DropColumn(
                name: "PanOrTaxId",
                table: "OwnerVerificationRequests");

            migrationBuilder.RenameColumn(
                name: "IsIdentityVerified",
                table: "Users",
                newName: "IsOwnerVerified");

            migrationBuilder.AddColumn<bool>(
                name: "IsFinanciallyVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsKycVerified",
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
                name: "KycVerifications",
                columns: table => new
                {
                    KycVerificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AadhaarNumber = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    PanNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KycVerificationId", x => x.KycVerificationId);
                    table.ForeignKey(
                        name: "FK_KycVerifications_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KycVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantFinancialProfiles",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmployerName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    DeclaredMonthlyIncome = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
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
                name: "KycDocuments",
                columns: table => new
                {
                    KycVerificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KycDocument", x => new { x.KycVerificationId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_KycDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KycDocuments_KycVerifications_KycVerificationId",
                        column: x => x.KycVerificationId,
                        principalTable: "KycVerifications",
                        principalColumn: "KycVerificationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantFinancialDocuments",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
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
                columns: new[] { "IsKycVerified", "IsOwnerVerified", "VerifiedMonthlyIncome" },
                values: new object[] { true, false, null });

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

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_DocumentId",
                table: "KycDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_KycVerifications_ReviewedById",
                table: "KycVerifications",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_KycVerifications_UserId",
                table: "KycVerifications",
                column: "UserId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KycDocuments");

            migrationBuilder.DropTable(
                name: "TenantFinancialDocuments");

            migrationBuilder.DropTable(
                name: "KycVerifications");

            migrationBuilder.DropTable(
                name: "TenantFinancialProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Users_AadhaarNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PanNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsFinanciallyVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsKycVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VerifiedMonthlyIncome",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RequiresVerifiedIncome",
                table: "Properties");

            migrationBuilder.RenameColumn(
                name: "IsOwnerVerified",
                table: "Users",
                newName: "IsIdentityVerified");

            migrationBuilder.AddColumn<string>(
                name: "PanOrTaxId",
                table: "OwnerVerificationRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IdentityVerificationRequests",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AadhaarNumber = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PanNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ReviewRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityVerificationRequestId", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_IdentityVerificationRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdentityVerificationRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OwnerProfiles",
                columns: table => new
                {
                    OwnerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PanOrTaxId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "TenantVerificationRequests",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReviewRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantVerificationRequestId", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_TenantVerificationRequests_RentalApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "RentalApplications",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantVerificationRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantVerificationRequests_Users_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityVerificationDocuments",
                columns: table => new
                {
                    IdentityVerificationDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityVerificationDocumentId", x => x.IdentityVerificationDocumentId);
                    table.ForeignKey(
                        name: "FK_IdentityVerificationDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdentityVerificationDocuments_IdentityVerificationRequests_~",
                        column: x => x.RequestId,
                        principalTable: "IdentityVerificationRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantVerificationDocuments",
                columns: table => new
                {
                    TenantVerificationDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantVerificationDocumentId", x => x.TenantVerificationDocumentId);
                    table.ForeignKey(
                        name: "FK_TenantVerificationDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantVerificationDocuments_TenantVerificationRequests_Requ~",
                        column: x => x.RequestId,
                        principalTable: "TenantVerificationRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "IsIdentityVerified",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityVerificationDocuments_DocumentId",
                table: "IdentityVerificationDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityVerificationDocuments_RequestId",
                table: "IdentityVerificationDocuments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityVerificationRequests_ReviewedById",
                table: "IdentityVerificationRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityVerificationRequests_UserId",
                table: "IdentityVerificationRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerProfiles_UserId",
                table: "OwnerProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantVerificationDocuments_DocumentId",
                table: "TenantVerificationDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantVerificationDocuments_RequestId",
                table: "TenantVerificationDocuments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantVerificationRequests_ApplicationId",
                table: "TenantVerificationRequests",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantVerificationRequests_ReviewedById",
                table: "TenantVerificationRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_TenantVerificationRequests_TenantId",
                table: "TenantVerificationRequests",
                column: "TenantId");
        }
    }
}
