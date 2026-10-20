using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballTransfer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLoan",
                table: "Transfers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLoanFee",
                table: "Transfers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FeeCurrency",
                table: "TransferNews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLoan",
                table: "TransferNews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLoanFee",
                table: "TransferNews",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLoan",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "IsLoanFee",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "FeeCurrency",
                table: "TransferNews");

            migrationBuilder.DropColumn(
                name: "IsLoan",
                table: "TransferNews");

            migrationBuilder.DropColumn(
                name: "IsLoanFee",
                table: "TransferNews");
        }
    }
}
