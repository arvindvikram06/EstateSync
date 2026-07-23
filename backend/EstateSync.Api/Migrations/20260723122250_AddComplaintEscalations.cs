using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintEscalations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EscalatedAt",
                table: "Complaints",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EscalationResolvedAt",
                table: "Complaints",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OwnerViewedAt",
                table: "Complaints",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComplaintEscalationContacts",
                columns: table => new
                {
                    ComplaintEscalationContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InternalNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContactedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintEscalationContactId", x => x.ComplaintEscalationContactId);
                    table.ForeignKey(
                        name: "FK_ComplaintEscalationContacts_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "ComplaintId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintEscalationContacts_ComplaintId_ContactedAt",
                table: "ComplaintEscalationContacts",
                columns: new[] { "ComplaintId", "ContactedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplaintEscalationContacts");

            migrationBuilder.DropColumn(
                name: "EscalatedAt",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "EscalationResolvedAt",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "OwnerViewedAt",
                table: "Complaints");
        }
    }
}
