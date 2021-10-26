using Microsoft.EntityFrameworkCore.Migrations;

namespace Puchase_and_payables.Migrations
{
    public partial class bankCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bank",
                table: "cor_bankaccountdetail");

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "cor_bankaccountdetail",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "cor_bankaccountdetail");

            migrationBuilder.AddColumn<int>(
                name: "Bank",
                table: "cor_bankaccountdetail",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
