using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballTransfer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFromAndToClub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FromClub",
                table: "TransferNews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToClub",
                table: "TransferNews",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromClub",
                table: "TransferNews");

            migrationBuilder.DropColumn(
                name: "ToClub",
                table: "TransferNews");
        }
    }
}
