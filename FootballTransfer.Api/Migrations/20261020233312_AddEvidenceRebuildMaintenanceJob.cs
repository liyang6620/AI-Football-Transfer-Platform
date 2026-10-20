using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FootballTransfer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceRebuildMaintenanceJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataMaintenanceJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ItemsProcessed = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataMaintenanceJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataMaintenanceJobs_Name",
                table: "DataMaintenanceJobs",
                column: "Name",
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE public."DataMaintenanceJobs" ENABLE ROW LEVEL SECURITY;

                UPDATE "TransferNews" SET "TransferId" = NULL;
                DELETE FROM "Transfers";

                UPDATE "TransferNews"
                SET
                    "IsProcessed" = FALSE,
                    "AiSummary" = NULL,
                    "ExtractedPlayer" = NULL,
                    "ExtractedClub" = NULL,
                    "FromClub" = NULL,
                    "ToClub" = NULL,
                    "TransferType" = NULL,
                    "EstimatedFee" = NULL,
                    "FeeCurrency" = NULL,
                    "IsLoan" = FALSE,
                    "IsLoanFee" = FALSE,
                    "Confidence" = NULL;

                INSERT INTO "DataMaintenanceJobs"
                    ("Name", "Status", "CreatedAt", "ItemsProcessed")
                VALUES
                    ('transfer-evidence-rebuild-2026-10-21', 'Pending', CURRENT_TIMESTAMP, 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataMaintenanceJobs");
        }
    }
}
