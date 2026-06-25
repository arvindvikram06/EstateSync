using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class LeaseESignAndAdminApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnsignedAgreementUrl",
                table: "Leases");

            migrationBuilder.AddColumn<DateTime>(
                name: "AdminApprovedAt",
                table: "Leases",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AdminApprovedById",
                table: "Leases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminRemarks",
                table: "Leases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerSignatureImageUrl",
                table: "Leases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerSignatureIp",
                table: "Leases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureVerificationHash",
                table: "Leases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantSignatureImageUrl",
                table: "Leases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantSignatureIp",
                table: "Leases",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminApprovedAt",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "AdminApprovedById",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "AdminRemarks",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "OwnerSignatureImageUrl",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "OwnerSignatureIp",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "SignatureVerificationHash",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "TenantSignatureImageUrl",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "TenantSignatureIp",
                table: "Leases");

            migrationBuilder.AddColumn<string>(
                name: "UnsignedAgreementUrl",
                table: "Leases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
