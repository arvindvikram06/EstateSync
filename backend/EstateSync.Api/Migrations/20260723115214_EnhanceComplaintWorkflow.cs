using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceComplaintWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReopenedAt",
                table: "Complaints",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReopenCount",
                table: "Complaints",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReopenReason",
                table: "Complaints",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolutionProposedAt",
                table: "Complaints",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionSummary",
                table: "Complaints",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Complaints",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComplaintActivities",
                columns: table => new
                {
                    ComplaintActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintActivityId", x => x.ComplaintActivityId);
                    table.ForeignKey(
                        name: "FK_ComplaintActivities_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "ComplaintId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "ComplaintActivities"
                    ("ComplaintActivityId", "ComplaintId", "ActorId", "ActorName", "ActorRole",
                     "ActivityType", "PreviousStatus", "NewStatus", "Summary", "CreatedAt")
                SELECT
                    gen_random_uuid(),
                    complaint."ComplaintId",
                    lease."TenantId",
                    tenant."FullName",
                    'Tenant',
                    'Created',
                    NULL,
                    'Open',
                    'Complaint submitted',
                    complaint."CreatedAt"
                FROM "Complaints" AS complaint
                INNER JOIN "Leases" AS lease ON lease."LeaseId" = complaint."LeaseId"
                INNER JOIN "Users" AS tenant ON tenant."UserId" = lease."TenantId";

                UPDATE "Complaints"
                SET "ResolvedAt" = COALESCE("UpdatedAt", "CreatedAt")
                WHERE "Status" IN ('Resolved', 'Closed');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintActivities_ComplaintId_CreatedAt",
                table: "ComplaintActivities",
                columns: new[] { "ComplaintId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplaintActivities");

            migrationBuilder.DropColumn(
                name: "LastReopenedAt",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "ReopenCount",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "ReopenReason",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "ResolutionProposedAt",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "ResolutionSummary",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Complaints");
        }
    }
}
