using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyVisitSlotsAndCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PreviousPropertyVisitSlotId",
                table: "SiteVisits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PropertyVisitSlotId",
                table: "SiteVisits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RescheduleReason",
                table: "SiteVisits",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RescheduledAt",
                table: "SiteVisits",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RescheduledByUserId",
                table: "SiteVisits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PropertyVisitSlots",
                columns: table => new
                {
                    PropertyVisitSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyVisitSlotId", x => x.PropertyVisitSlotId);
                    table.CheckConstraint("CK_PropertyVisitSlots_Duration", "\"DurationMinutes\" = 60");
                    table.ForeignKey(
                        name: "FK_PropertyVisitSlots_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "PropertyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "PropertyVisitSlots"
                    ("PropertyVisitSlotId", "PropertyId", "SlotDate", "StartTime", "DurationMinutes", "Status", "CreatedAt")
                SELECT
                    gen_random_uuid(),
                    "PropertyId",
                    "VisitDate",
                    "VisitTime",
                    60,
                    CASE
                        WHEN bool_or("Status" = 'Approved') THEN 'Booked'
                        WHEN bool_or("Status" = 'Pending') THEN 'Reserved'
                        ELSE 'Available'
                    END,
                    MIN("CreatedAt")
                FROM "SiteVisits"
                WHERE "DeletedAt" IS NULL
                GROUP BY "PropertyId", "VisitDate", "VisitTime";

                UPDATE "SiteVisits" AS visit
                SET "PropertyVisitSlotId" = slot."PropertyVisitSlotId"
                FROM "PropertyVisitSlots" AS slot
                WHERE slot."PropertyId" = visit."PropertyId"
                  AND slot."SlotDate" = visit."VisitDate"
                  AND slot."StartTime" = visit."VisitTime";

                UPDATE "SiteVisits"
                SET "Status" = 'PendingOwnerApproval'
                WHERE "Status" = 'Pending';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_PreviousPropertyVisitSlotId",
                table: "SiteVisits",
                column: "PreviousPropertyVisitSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_PropertyVisitSlotId",
                table: "SiteVisits",
                column: "PropertyVisitSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyVisitSlots_PropertyId_SlotDate_StartTime",
                table: "PropertyVisitSlots",
                columns: new[] { "PropertyId", "SlotDate", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyVisitSlots_PropertyId_Status_SlotDate",
                table: "PropertyVisitSlots",
                columns: new[] { "PropertyId", "Status", "SlotDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_SiteVisits_PropertyVisitSlots_PreviousPropertyVisitSlotId",
                table: "SiteVisits",
                column: "PreviousPropertyVisitSlotId",
                principalTable: "PropertyVisitSlots",
                principalColumn: "PropertyVisitSlotId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteVisits_PropertyVisitSlots_PropertyVisitSlotId",
                table: "SiteVisits",
                column: "PropertyVisitSlotId",
                principalTable: "PropertyVisitSlots",
                principalColumn: "PropertyVisitSlotId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "SiteVisits"
                SET "Status" = 'Pending'
                WHERE "Status" IN ('PendingOwnerApproval', 'PendingTenantApproval');
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_SiteVisits_PropertyVisitSlots_PreviousPropertyVisitSlotId",
                table: "SiteVisits");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteVisits_PropertyVisitSlots_PropertyVisitSlotId",
                table: "SiteVisits");

            migrationBuilder.DropTable(
                name: "PropertyVisitSlots");

            migrationBuilder.DropIndex(
                name: "IX_SiteVisits_PreviousPropertyVisitSlotId",
                table: "SiteVisits");

            migrationBuilder.DropIndex(
                name: "IX_SiteVisits_PropertyVisitSlotId",
                table: "SiteVisits");

            migrationBuilder.DropColumn(
                name: "PreviousPropertyVisitSlotId",
                table: "SiteVisits");

            migrationBuilder.DropColumn(
                name: "PropertyVisitSlotId",
                table: "SiteVisits");

            migrationBuilder.DropColumn(
                name: "RescheduleReason",
                table: "SiteVisits");

            migrationBuilder.DropColumn(
                name: "RescheduledAt",
                table: "SiteVisits");

            migrationBuilder.DropColumn(
                name: "RescheduledByUserId",
                table: "SiteVisits");
        }
    }
}
