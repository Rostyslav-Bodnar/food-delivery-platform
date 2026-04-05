using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.PaymentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFundsFlowToPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FundsFlow",
                table: "payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FundsFlow",
                table: "payments");
        }
    }
}
