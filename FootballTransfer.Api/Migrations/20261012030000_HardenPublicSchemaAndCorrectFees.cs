using FootballTransfer.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballTransfer.Api.Migrations;

[DbContext(typeof(FootballTransferDbContext))]
[Migration("20261012030000_HardenPublicSchemaAndCorrectFees")]
public partial class HardenPublicSchemaAndCorrectFees : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "TransferNews" AS news
            SET "EstimatedFee" = CASE
                WHEN news."Title" ILIKE '%800 000%' THEN 0.8
                WHEN news."Title" ILIKE '%500,000%' THEN 0.5
                WHEN news."Content" ILIKE '%GBP 500,000%' OR news."Content" ILIKE '%£500,000%' THEN 0.5
                WHEN news."Content" ILIKE '%GBP 300,000%' OR news."Content" ILIKE '%£300,000%' THEN 0.3
                ELSE news."EstimatedFee"
            END
            WHERE news."EstimatedFee" >= 300;

            UPDATE "Transfers" AS transfer
            SET "EstimatedFee" = news."EstimatedFee"
            FROM "TransferNews" AS news
            WHERE transfer."TransferNewsId" = news."Id"
              AND transfer."EstimatedFee" >= 300
              AND news."EstimatedFee" < 300;

            ALTER TABLE public."Clubs" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE public."Players" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE public."TransferNews" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE public."Transfers" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE public."__EFMigrationsHistory" ENABLE ROW LEVEL SECURITY;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE public."Clubs" DISABLE ROW LEVEL SECURITY;
            ALTER TABLE public."Players" DISABLE ROW LEVEL SECURITY;
            ALTER TABLE public."TransferNews" DISABLE ROW LEVEL SECURITY;
            ALTER TABLE public."Transfers" DISABLE ROW LEVEL SECURITY;
            ALTER TABLE public."__EFMigrationsHistory" DISABLE ROW LEVEL SECURITY;
            """);
    }
}
