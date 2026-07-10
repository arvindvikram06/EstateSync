using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRentIncreaseProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentIncreaseProposals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RentIncreaseProposals",
                columns: table => new
                {
                    ProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CurrentRent = table.Column<decimal>(type: "numeric", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NewRent = table.Column<decimal>(type: "numeric", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalId", x => x.ProposalId);
                    table.ForeignKey(
                        name: "FK_RentIncreaseProposals_Leases_LeaseId",
                        column: x => x.LeaseId,
                        principalTable: "Leases",
                        principalColumn: "LeaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentIncreaseProposals_Users_ProposedById",
                        column: x => x.ProposedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentIncreaseProposals_LeaseId",
                table: "RentIncreaseProposals",
                column: "LeaseId");

            migrationBuilder.CreateIndex(
                name: "IX_RentIncreaseProposals_ProposedById",
                table: "RentIncreaseProposals",
                column: "ProposedById");
        }
    }
}
