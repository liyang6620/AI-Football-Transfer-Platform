using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballTransfer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFieldsToNews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "TransferNews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProcessed",
                table: "TransferNews",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "TransferNews");

            migrationBuilder.DropColumn(
                name: "IsProcessed",
                table: "TransferNews");
        }
    }
}
