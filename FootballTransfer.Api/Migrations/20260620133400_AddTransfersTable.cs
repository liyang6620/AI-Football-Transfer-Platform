using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballTransfer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransfersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fee",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "FromClubId",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Transfers");

            migrationBuilder.RenameColumn(
                name: "ToClubId",
                table: "Transfers",
                newName: "TransferNewsId");

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "Transfers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Transfers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedFee",
                table: "Transfers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromClub",
                table: "Transfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlayerName",
                table: "Transfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToClub",
                table: "Transfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferType",
                table: "Transfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_TransferNewsId",
                table: "Transfers",
                column: "TransferNewsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_TransferNews_TransferNewsId",
                table: "Transfers",
                column: "TransferNewsId",
                principalTable: "TransferNews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_TransferNews_TransferNewsId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_TransferNewsId",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "EstimatedFee",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "FromClub",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "PlayerName",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ToClub",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "TransferType",
                table: "Transfers");

            migrationBuilder.RenameColumn(
                name: "TransferNewsId",
                table: "Transfers",
                newName: "ToClubId");

            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "Transfers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "FromClubId",
                table: "Transfers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlayerId",
                table: "Transfers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Transfers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
