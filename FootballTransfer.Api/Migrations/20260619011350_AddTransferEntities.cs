using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballTransfer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "TransferNews",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedFee",
                table: "TransferNews",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedClub",
                table: "TransferNews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedPlayer",
                table: "TransferNews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferType",
                table: "TransferNews",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "TransferNews");

            migrationBuilder.DropColumn(
                name: "EstimatedFee",
                table: "TransferNews");

            migrationBuilder.DropColumn(
                name: "ExtractedClub",
                table: "TransferNews");

            migrationBuilder.DropColumn(
                name: "ExtractedPlayer",
                table: "TransferNews");

            migrationBuilder.DropColumn(
                name: "TransferType",
                table: "TransferNews");
        }
    }
}
