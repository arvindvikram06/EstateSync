using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOwnerVerificationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwnerVerificationDocuments");

            migrationBuilder.DropTable(
                name: "OwnerVerificationRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OwnerVerificationRequests",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReviewRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerVerificationRequestId", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_OwnerVerificationRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerVerificationRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OwnerVerificationDocuments",
                columns: table => new
                {
                    OwnerVerificationDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerVerificationDocumentId", x => x.OwnerVerificationDocumentId);
                    table.ForeignKey(
                        name: "FK_OwnerVerificationDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerVerificationDocuments_OwnerVerificationRequests_Reques~",
                        column: x => x.RequestId,
                        principalTable: "OwnerVerificationRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerVerificationDocuments_DocumentId",
                table: "OwnerVerificationDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerVerificationDocuments_RequestId",
                table: "OwnerVerificationDocuments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerVerificationRequests_ReviewedById",
                table: "OwnerVerificationRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerVerificationRequests_UserId",
                table: "OwnerVerificationRequests",
                column: "UserId");
        }
    }
}
