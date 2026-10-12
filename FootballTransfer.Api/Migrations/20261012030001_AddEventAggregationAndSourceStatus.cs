using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FootballTransfer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAggregationAndSourceStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_TransferNews_TransferNewsId",
                table: "Transfers");

            migrationBuilder.AddColumn<string>(
                name: "EventKey",
                table: "Transfers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TransferId",
                table: "TransferNews",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SourceSyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSuccessAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ItemsSeen = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceSyncStates", x => x.Id);
                });

            migrationBuilder.Sql("""
                UPDATE "Transfers"
                SET "EventKey" =
                    regexp_replace(lower(coalesce("PlayerName", '')), '[^[:alnum:]]', '', 'g') || '|' ||
                    CASE regexp_replace(lower(coalesce("ToClub", '')), '[^[:alnum:]]', '', 'g')
                        WHEN 'manutd' THEN 'manchesterunited'
                        WHEN 'manchesterutd' THEN 'manchesterunited'
                        WHEN 'manunited' THEN 'manchesterunited'
                        WHEN 'mancity' THEN 'manchestercity'
                        WHEN 'spurs' THEN 'tottenhamhotspur'
                        WHEN 'tottenham' THEN 'tottenhamhotspur'
                        WHEN 'psg' THEN 'parissaintgermain'
                        WHEN 'inter' THEN 'intermilan'
                        WHEN 'internazionale' THEN 'intermilan'
                        WHEN 'acmilan' THEN 'milan'
                        ELSE regexp_replace(lower(coalesce("ToClub", '')), '[^[:alnum:]]', '', 'g')
                    END || '|' ||
                    CASE WHEN lower(coalesce("TransferType", '')) = 'contract' THEN 'contract' ELSE 'movement' END;

                DELETE FROM "Transfers" AS transfer
                USING "TransferNews" AS news
                WHERE transfer."TransferNewsId" = news."Id"
                  AND (
                    news."Title" ILIKE 'how manzambi became one of europe%'
                    OR news."Title" ILIKE 'a team that offered hope then%'
                    OR news."Title" ILIKE 'gordon a better fit for barca%'
                    OR news."Title" ILIKE 'west ham go top of championship%'
                    OR news."Title" ILIKE 'bouaddi impresses on first start%'
                    OR news."Title" ILIKE 'breaking down chelsea%summer exits%'
                    OR news."Title" ILIKE 'barcola%grasshopper%overcome setbacks%'
                    OR news."Title" ILIKE 'signs that jaissle%not to be written off%'
                    OR news."Title" ILIKE '%plans for life after football%'
                    OR news."Title" ILIKE 'breakouts%wildcards%surprise stars%'
                  );

                WITH ranked AS (
                    SELECT
                        "Id",
                        "TransferNewsId",
                        first_value("Id") OVER (
                            PARTITION BY "EventKey"
                            ORDER BY "Confidence" DESC NULLS LAST, "PublishedAt" DESC, "Id" DESC
                        ) AS keeper_id,
                        row_number() OVER (
                            PARTITION BY "EventKey"
                            ORDER BY "Confidence" DESC NULLS LAST, "PublishedAt" DESC, "Id" DESC
                        ) AS row_number
                    FROM "Transfers"
                )
                UPDATE "TransferNews" AS news
                SET "TransferId" = ranked.keeper_id
                FROM ranked
                WHERE news."Id" = ranked."TransferNewsId";

                WITH ranked AS (
                    SELECT
                        "Id",
                        row_number() OVER (
                            PARTITION BY "EventKey"
                            ORDER BY "Confidence" DESC NULLS LAST, "PublishedAt" DESC, "Id" DESC
                        ) AS row_number
                    FROM "Transfers"
                )
                DELETE FROM "Transfers" AS transfer
                USING ranked
                WHERE transfer."Id" = ranked."Id" AND ranked.row_number > 1;

                ALTER TABLE public."SourceSyncStates" ENABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_EventKey",
                table: "Transfers",
                column: "EventKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferNews_TransferId",
                table: "TransferNews",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceSyncStates_Name",
                table: "SourceSyncStates",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TransferNews_Transfers_TransferId",
                table: "TransferNews",
                column: "TransferId",
                principalTable: "Transfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_TransferNews_TransferNewsId",
                table: "Transfers",
                column: "TransferNewsId",
                principalTable: "TransferNews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransferNews_Transfers_TransferId",
                table: "TransferNews");

            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_TransferNews_TransferNewsId",
                table: "Transfers");

            migrationBuilder.DropTable(
                name: "SourceSyncStates");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_EventKey",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_TransferNews_TransferId",
                table: "TransferNews");

            migrationBuilder.DropColumn(
                name: "EventKey",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "TransferId",
                table: "TransferNews");

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_TransferNews_TransferNewsId",
                table: "Transfers",
                column: "TransferNewsId",
                principalTable: "TransferNews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
