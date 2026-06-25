using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityVerificationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityVerificationRequests",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AadhaarNumber = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    PanNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "IdentityVerificationDocuments",
                columns: table => new
                {
                    IdentityVerificationDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityVerificationDocuments");

            migrationBuilder.DropTable(
                name: "IdentityVerificationRequests");
        }
    }
}
