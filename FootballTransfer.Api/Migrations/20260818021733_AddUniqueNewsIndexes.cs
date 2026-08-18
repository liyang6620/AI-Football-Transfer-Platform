using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballTransfer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueNewsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_TransferNewsId",
                table: "Transfers");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_TransferNewsId",
                table: "Transfers",
                column: "TransferNewsId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferNews_Url",
                table: "TransferNews",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_TransferNewsId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_TransferNews_Url",
                table: "TransferNews");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_TransferNewsId",
                table: "Transfers",
                column: "TransferNewsId");
        }
    }
}
